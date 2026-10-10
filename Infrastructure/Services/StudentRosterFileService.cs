using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Domain.Services;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Requests;

namespace Infrastructure.Services;

public class StudentRosterFileService : IStudentRosterFileService
{
    public const int MaxUploadBytes = 5 * 1024 * 1024;
    private static readonly XNamespace Sheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    public IReadOnlyList<StudentImportRow> ReadImport(byte[] bytes, string fileName)
    {
        if (bytes.Length == 0 || bytes.Length > MaxUploadBytes) throw Invalid("File must contain data and be at most 5 MiB.");
        try
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var rows = extension switch
            {
                ".csv" => ReadCsv(new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF')),
                ".xlsx" => ReadExcel(bytes),
                _ => throw Invalid("Use a UTF-8 CSV or XLSX file.")
            };
            if (rows.Count == 0) throw Invalid("Header row is required.");
            var headers = rows[0].Select(x => x.Trim().ToLowerInvariant()).ToArray();
            if (headers.Length > 64 || headers.Distinct().Count() != headers.Length ||
                new[] { "email", "gradeid", "classid" }.Any(x => !headers.Contains(x))) throw Invalid("Required unique columns: email, gradeId, classId.");
            var result = new List<StudentImportRow>();
            for (var i = 1; i < rows.Count; i++)
            {
                if (rows[i].All(string.IsNullOrWhiteSpace)) continue;
                if (rows[i].Length > headers.Length) throw Invalid("Row contains more cells than headers.");
                string Cell(string name) { var index = Array.IndexOf(headers, name); return index < rows[i].Length ? rows[i][index].Trim() : ""; }
                result.Add(new StudentImportRow { RowNumber = i + 1, Email = Cell("email"), GradeId = Cell("gradeid"), ClassId = Cell("classid") });
            }
            if (result.Count == 0 || result.Count > 1000) throw Invalid("Provide between 1 and 1000 student rows.");
            return result;
        }
        catch (Exception ex) when (ex is XmlException or InvalidDataException or DecoderFallbackException or FormatException or OverflowException or ArgumentException)
        { throw Invalid("Malformed student file."); }
    }

    public byte[] Write(string[] headers, IReadOnlyList<string[]> rows, string format)
    {
        if (format == "csv")
        {
            string Quote(string value)
            {
                if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            var text = string.Join("\r\n", new[] { headers }.Concat(rows).Select(row => string.Join(',', row.Select(Quote)))) + "\r\n";
            return new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
        }
        if (format != "xlsx") throw Invalid("format must be csv or xlsx.");
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        {
            void Entry(string path, string content) { using var writer = new StreamWriter(archive.CreateEntry(path).Open(), new UTF8Encoding(false)); writer.Write(content); }
            Entry("[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            Entry("_rels/.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            Entry("xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Students\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Entry("xl/_rels/workbook.xml.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var sheetData = new XElement(Sheet + "sheetData");
            var index = 0;
            foreach (var row in new[] { headers }.Concat(rows))
            {
                index++;
                sheetData.Add(new XElement(Sheet + "row", new XAttribute("r", index), row.Select((value, col) =>
                    new XElement(Sheet + "c", new XAttribute("r", ColumnName(col + 1) + index), new XAttribute("t", "inlineStr"),
                        new XElement(Sheet + "is", new XElement(Sheet + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), value))))));
            }
            Entry("xl/worksheets/sheet1.xml", new XElement(Sheet + "worksheet", sheetData).ToString(SaveOptions.DisableFormatting));
        }
        return buffer.ToArray();
    }

    private static List<string[]> ReadCsv(string text)
    {
        var rows = new List<string[]>(); var cells = new List<string>(); var cell = new StringBuilder();
        var quoted = false; var closed = false;
        void EndCell() { cells.Add(cell.ToString()); cell.Clear(); closed = false; if (cells.Count > 64) throw Invalid("Too many columns."); }
        void EndRow() { EndCell(); rows.Add(cells.ToArray()); cells.Clear(); if (rows.Count > 1001) throw Invalid("Too many rows."); }
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else { quoted = false; closed = true; } }
                else cell.Append(c);
            }
            else if (c == ',') EndCell();
            else if (c == '\r' || c == '\n') { EndRow(); if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++; }
            else if (c == '"' && cell.Length == 0 && !closed) quoted = true;
            else { if (closed || c == '"') throw Invalid("Malformed CSV quoting."); cell.Append(c); }
            if (cell.Length > 4096) throw Invalid("Cell text is too long.");
        }
        if (quoted) throw Invalid("Unclosed CSV quote.");
        if (cell.Length > 0 || cells.Count > 0 || closed) EndRow();
        return rows;
    }

    private static List<string[]> ReadExcel(byte[] bytes)
    {
        using var buffer = new MemoryStream(bytes);
        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read);
        if (archive.Entries.Count > 128 || archive.Entries.Sum(x => x.Length) > 20 * 1024 * 1024 ||
            archive.Entries.Select(x => x.FullName).Distinct().Count() != archive.Entries.Count ||
            archive.Entries.Any(x => x.FullName.Contains("vbaProject", StringComparison.OrdinalIgnoreCase))) throw Invalid("Unsupported or oversized workbook.");
        XDocument Xml(string name)
        {
            var entry = archive.GetEntry(name) ?? throw Invalid("Workbook is missing a required part.");
            using var stream = entry.Open();
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 20 * 1024 * 1024 });
            return XDocument.Load(reader);
        }
        var workbook = Xml("xl/workbook.xml");
        var first = workbook.Descendants(Sheet + "sheet").FirstOrDefault() ?? throw Invalid("Workbook has no worksheet.");
        var rid = (string?)first.Attribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"));
        var rel = Xml("xl/_rels/workbook.xml.rels").Descendants().FirstOrDefault(x => x.Name.LocalName == "Relationship" && (string?)x.Attribute("Id") == rid);
        var target = (string?)rel?.Attribute("Target") ?? throw Invalid("Worksheet relationship is missing.");
        if ((string?)rel?.Attribute("TargetMode") == "External" || target.Contains("..") || target.Contains('\\')) throw Invalid("Invalid worksheet relationship.");
        var path = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
        if (!path.StartsWith("xl/worksheets/", StringComparison.Ordinal)) throw Invalid("Invalid worksheet path.");
        var strings = archive.GetEntry("xl/sharedStrings.xml") == null ? Array.Empty<string>() :
            Xml("xl/sharedStrings.xml").Descendants(Sheet + "si").Select(x => string.Concat(x.Descendants(Sheet + "t").Select(t => t.Value))).ToArray();
        var result = new List<string[]>();
        var worksheet = Xml(path);
        int Reference(string reference, int? expectedRow = null)
        {
            var match = System.Text.RegularExpressions.Regex.Match(reference, "^([A-Z]{1,3})([1-9][0-9]{0,6})$");
            if (!match.Success || !int.TryParse(match.Groups[2].Value, out var number) || number > 1001 ||
                (expectedRow.HasValue && expectedRow != number)) throw Invalid("Invalid or excessive worksheet reference.");
            var column = 0;
            foreach (var letter in match.Groups[1].Value) column = checked(column * 26 + letter - 'A' + 1);
            if (column > 64) throw Invalid("Too many columns.");
            return column;
        }
        var dimension = (string?)worksheet.Root?.Element(Sheet + "dimension")?.Attribute("ref");
        if (dimension != null)
        {
            var range = dimension.Split(':');
            if (range.Length is < 1 or > 2) throw Invalid("Invalid worksheet dimension.");
            foreach (var reference in range) Reference(reference);
        }
        foreach (var row in worksheet.Descendants(Sheet + "sheetData").Elements(Sheet + "row"))
        {
            var number = (int?)row.Attribute("r") ?? result.Count + 1;
            if (result.Count >= 1001 || number <= result.Count || number > 1001) throw Invalid("Invalid or excessive rows.");
            var cells = new SortedDictionary<int, string>(); var sequential = 0;
            foreach (var c in row.Elements(Sheet + "c"))
            {
                if (c.Element(Sheet + "f") != null) throw Invalid("Formula cells are not supported in imports.");
                var reference = (string?)c.Attribute("r"); var column = 0;
                if (reference != null) column = Reference(reference, number);
                else column = sequential + 1;
                if (column < 1 || column > 64 || cells.ContainsKey(column)) throw Invalid("Invalid or excessive columns.");
                sequential = column;
                var type = (string?)c.Attribute("t");
                var value = c.Element(Sheet + "v")?.Value ?? "";
                if (type == "s") { if (!int.TryParse(value, out var idx) || idx < 0 || idx >= strings.Length) throw Invalid("Invalid shared string."); value = strings[idx]; }
                else if (type == "inlineStr") value = string.Concat(c.Descendants(Sheet + "t").Select(x => x.Value));
                if (value.Length > 4096) throw Invalid("Cell text is too long.");
                cells[column] = value;
            }
            // Preserve worksheet row positions, including blank gaps, for useful row-error numbers.
            while (result.Count < number - 1) result.Add(Array.Empty<string>());
            result.Add(Enumerable.Range(1, cells.Count == 0 ? 0 : cells.Keys.Max()).Select(i => cells.GetValueOrDefault(i, "")).ToArray());
        }
        return result;
    }

    private static string ColumnName(int index)
    { var result = ""; while (index > 0) { index--; result = (char)('A' + index % 26) + result; index /= 26; } return result; }

    private static GenericException Invalid(string message) => new(ErrorCode.ValidationError, message, System.Net.HttpStatusCode.BadRequest);
}
