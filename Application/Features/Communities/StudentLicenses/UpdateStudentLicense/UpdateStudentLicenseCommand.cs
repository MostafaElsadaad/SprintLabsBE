using Application.Features.Communities.StudentLicenses.Common;

using MediatR;

namespace Application.Features.Communities.StudentLicenses.UpdateStudentLicense;

public class UpdateStudentLicenseCommand : IRequest<StudentLicenseResponse>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public long LicenseId { get; set; }
    public string Email { get; set; } = string.Empty;
    public long GradeId { get; set; }
    public long ClassId { get; set; }
}
