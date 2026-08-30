using Application.Features.Communities.Teachers.Common;
using MediatR;
using Shared.Requests;
using Shared.Responses;

namespace Application.Features.Communities.Teachers.ListTeachers;

public class ListTeachersQuery : PagedRequest, IRequest<PagedResponse<TeacherResponse>>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public long? GradeId { get; set; }
    public long? ClassId { get; set; }
    public string? Search { get; set; }
    public Domain.Enums.CommunityUserStatus? Status { get; set; }
}
