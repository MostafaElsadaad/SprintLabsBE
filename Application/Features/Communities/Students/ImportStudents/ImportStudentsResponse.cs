namespace Application.Features.Communities.Students.ImportStudents;

public class ImportStudentsResponse
{
    public int TotalRows => Rows.Count;
    public int Imported => Rows.Count(x => x.Outcome == "IMPORTED");
    public int Skipped => Rows.Count(x => x.Outcome == "SKIPPED");
    public int Failed => Rows.Count(x => x.Outcome == "FAILED");
    public List<ImportStudentResult> Rows { get; set; } = new();
}
