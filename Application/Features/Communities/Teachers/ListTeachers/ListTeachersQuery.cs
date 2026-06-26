using Application.Features.Communities.Teachers.Common;

using MediatR;

namespace Application.Features.Communities.Teachers.ListTeachers;

public class ListTeachersQuery : IRequest<List<TeacherResponse>>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
}
