namespace Application.Features.Communities.StudentLicenses.UpdateStudentLicense;

public class UpdateStudentLicenseRequest
{
    public string Email { get; set; } = string.Empty;
    public long GradeId { get; set; }
    public long ClassId { get; set; }
}
