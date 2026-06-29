namespace Application.Features.Communities.StudentLicenses.AddStudentLicense;

public class AddStudentLicenseRequest
{
    public string Email { get; set; } = string.Empty;
    public long GradeId { get; set; }
    public long ClassId { get; set; }
}
