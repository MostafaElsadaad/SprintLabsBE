using MediatR;

namespace Application.Features.Admin.Communities.UpsertCommunityLicense;

public class UpsertCommunityLicenseCommand : IRequest<CommunityLicenseResponse>
{
    public long AuthenticatedUserId { get; set; }
    public long CommunityId { get; set; }
    public int MaxStudents { get; set; }
    public int MaxTeachers { get; set; }
    public int StudentEmailChangeLimit { get; set; }
}
