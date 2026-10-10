using System.Text;
using Infrastructure.DataAccess;
using Xunit;
using System.IO.Compression;
using Infrastructure.Services;
using Shared.Exceptions;

namespace Compass.Tests.Features.CommunityDashboard;

public class StudentFileTests
{
    private static dynamic Codec()
    {
        var type = typeof(ApplicationDbContext).Assembly.GetType("Infrastructure.Services.StudentRosterFileService");
        Assert.NotNull(type);
        return Activator.CreateInstance(type)!;
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("xlsx")]
    public void Files_RoundTripUnicodeQuotedCommaAndMultiline(string format)
    {
        dynamic codec = Codec();
        string[] headers = ["email", "gradeId", "classId"];
        IReadOnlyList<string[]> rows = new List<string[]> { new[] { "qa@example.com", "107", "1" },
            new[] { "Unicode é,quoted\"\nname@example.com", "108", "2" } };
        byte[] file = codec.Write(headers, rows, format);
        var parsed = codec.ReadImport(file, "students." + format);
        Assert.Equal(2, (int)parsed.Count);
        Assert.Equal(rows[1][0], (string)parsed[1].Email);
    }

    [Fact]
    public void Csv_RejectsMalformedHeadersAndProtectsFormulaText()
    {
        dynamic codec = Codec();
        Assert.ThrowsAny<Exception>(() => codec.ReadImport(Encoding.UTF8.GetBytes("email,email,classId\na,b,1"), "students.csv"));
        IReadOnlyList<string[]> rows = new List<string[]> { new[] { "=HYPERLINK(\"bad\")" } };
        byte[] bytes = codec.Write(new[] { "name" }, rows, "csv");
        Assert.Contains("'=HYPERLINK", Encoding.UTF8.GetString(bytes));
    }

    [Theory]
    [InlineData("email,gradeId,classId\n\"unclosed,107,1")]
    [InlineData("email,gradeId,classId\n\"qa@example.com\"oops,107,1")]
    [InlineData("email,gradeId,classId\na,b,1,unexpected")]
    public void Csv_RejectsInvalidStructureBeforeEnrollment(string text)
    {
        Assert.Throws<GenericException>(() => new StudentRosterFileService().ReadImport(Encoding.UTF8.GetBytes(text), "students.csv"));
    }

    [Fact]
    public void Files_RejectUnsupportedEmptyOversizeAndTooManyRows()
    {
        var codec = new StudentRosterFileService();
        Assert.Throws<GenericException>(() => codec.ReadImport(Array.Empty<byte>(), "a.csv"));
        Assert.Throws<GenericException>(() => codec.ReadImport(new byte[5 * 1024 * 1024 + 1], "a.csv"));
        Assert.Throws<GenericException>(() => codec.ReadImport(new byte[] { 1 }, "a.xls"));
        var csv = "email,gradeId,classId\n" + string.Concat(Enumerable.Repeat("qa@example.com,107,1\n", 1001));
        Assert.Throws<GenericException>(() => codec.ReadImport(Encoding.UTF8.GetBytes(csv), "a.csv"));
    }

    [Theory]
    [InlineData("formula")]
    [InlineData("dtd")]
    [InlineData("expansion")]
    [InlineData("external")]
    [InlineData("reference")]
    public void Excel_RejectsFormulasDtdExpansionAndExternalWorksheet(string attack)
    {
        var codec = new StudentRosterFileService();
        var original = codec.Write(new[] { "email", "gradeId", "classId" }, new List<string[]> { new[] { "qa@example.com", "107", "1" } }, "xlsx");
        using var buffer = new MemoryStream(); buffer.Write(original); buffer.Position = 0;
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Update, true))
        {
            var path = attack == "external" ? "xl/_rels/workbook.xml.rels" : "xl/worksheets/sheet1.xml";
            var entry = zip.GetEntry(path)!;
            string xml; using (var reader = new StreamReader(entry.Open())) xml = reader.ReadToEnd();
            entry.Delete();
            if (attack == "formula") xml = xml.Replace("<is>", "<f>1+1</f><is>");
            if (attack == "dtd") xml = "<!DOCTYPE worksheet [<!ENTITY test SYSTEM 'file:///does-not-exist'>]>" + xml;
            if (attack == "expansion") xml += new string(' ', 21 * 1024 * 1024);
            if (attack == "external") xml = xml.Replace("Target=", "TargetMode=\"External\" Target=");
            if (attack == "reference") xml = xml.Replace("r=\"A2\"", "r=\"A9999\"");
            using var writer = new StreamWriter(zip.CreateEntry(path).Open()); writer.Write(xml);
        }
        Assert.Throws<GenericException>(() => codec.ReadImport(buffer.ToArray(), "students.xlsx"));
    }
}
