using Application.Features.Communities.Teachers.Common;
using MediatR;

namespace Application.Features.Communities.Teachers.ReplaceTeacherClassAssignments;

public class ReplaceTeacherClassAssignmentsCommand : IRequest<TeacherResponse>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public long TeacherUserId { get; set; }
    public List<long> ClassIds { get; set; } = new();
}
