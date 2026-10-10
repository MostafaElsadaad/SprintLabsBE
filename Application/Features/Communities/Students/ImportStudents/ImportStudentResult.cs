namespace Application.Features.Communities.Students.ImportStudents;

public class ImportStudentResult
{
    public int RowNumber { get; set; }
    public string Email { get; set; } = "";
    public string Outcome { get; set; } = "";
    public long? LicenseId { get; set; }
    public string? ReasonCode { get; set; }
    public string? Reason { get; set; }
}
