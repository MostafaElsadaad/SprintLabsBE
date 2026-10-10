"""Generate disposable CSV/XLSX upload fixtures using actual community grade/class IDs."""
import argparse
import csv
import io
import uuid
import zipfile
from pathlib import Path
from xml.etree.ElementTree import Element, SubElement, tostring


def prepare(grade_id, class_id, output):
    if not 0 < grade_id <= 2**63 - 1 or not 0 < class_id <= 2**63 - 1:
        raise ValueError("Use positive Int64 grade/class IDs from collection request 04.")
    tag = "postman-xlsx-" + uuid.uuid4().hex
    rows = [["email", "gradeId", "classId"],
            [tag + "-a@example.invalid", str(grade_id), str(class_id)],
            [tag + "-b@example.invalid", str(grade_id), str(class_id)],
            [tag + "-a@example.invalid", str(grade_id), str(class_id)],
            ["invalid-email", str(grade_id), str(class_id)]]
    output.mkdir(parents=True, exist_ok=True)
    with (output / "students-import.csv").open("w", encoding="utf-8-sig", newline="") as file:
        csv.writer(file).writerows(rows)
    ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    sheet = Element("worksheet", xmlns=ns)
    data = SubElement(sheet, "sheetData")
    for index, values in enumerate(rows, 1):
        row = SubElement(data, "row", r=str(index))
        for col, value in enumerate(values):
            cell = SubElement(row, "c", r=chr(ord("A") + col) + str(index), t="inlineStr")
            SubElement(SubElement(cell, "is"), "t").text = value
    with zipfile.ZipFile(output / "students-import.xlsx", "w", compression=zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("[Content_Types].xml", '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>')
        archive.writestr("_rels/.rels", '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>')
        archive.writestr("xl/workbook.xml", '<workbook xmlns="' + ns + '" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Students" sheetId="1" r:id="rId1"/></sheets></workbook>')
        archive.writestr("xl/_rels/workbook.xml.rels", '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>')
        archive.writestr("xl/worksheets/sheet1.xml", tostring(sheet, encoding="utf-8", xml_declaration=True))
    return output / "students-import.xlsx"


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--grade-id", type=int, required=True)
    parser.add_argument("--class-id", type=int, required=True)
    parser.add_argument("--output", type=Path, default=Path(__file__).parent / "local-fixtures")
    args = parser.parse_args()
    print(prepare(args.grade_id, args.class_id, args.output).resolve())
