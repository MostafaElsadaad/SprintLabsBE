using Application.Features.Communities.StudentLicenses.Common;

using MediatR;

namespace Application.Features.Communities.StudentLicenses.RevokeStudentLicense;

public class RevokeStudentLicenseCommand : IRequest<StudentLicenseResponse>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public long LicenseId { get; set; }
}
