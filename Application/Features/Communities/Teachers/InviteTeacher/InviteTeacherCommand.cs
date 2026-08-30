using Application.Features.Communities.Teachers.Common;

using MediatR;

namespace Application.Features.Communities.Teachers.InviteTeacher;

public class InviteTeacherCommand : IRequest<TeacherResponse>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public string Email { get; set; } = string.Empty;
}
