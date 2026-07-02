using Application.Features.Communities.Students.Common;

using MediatR;

namespace Application.Features.Communities.Students.GetStudentDetail;

public class GetStudentDetailQuery : IRequest<CommunityStudentDetailResponse>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public long PlayerProfileId { get; set; }
}
