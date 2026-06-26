using Application.Features.Communities.Teachers.Common;

using MediatR;

namespace Application.Features.Communities.Teachers.RemoveTeacher;

public class RemoveTeacherCommand : IRequest<TeacherResponse>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public long TeacherUserId { get; set; }
}
