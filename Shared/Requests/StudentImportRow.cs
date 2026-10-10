namespace Shared.Requests;

public class StudentImportRow
{
    public int RowNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string GradeId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
}
