using MediatR;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.ListDashboardTeachers;

public class ListDashboardTeachersQuery : IRequest<PagedResponse<TeacherView>>
{
    public long UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public long? GradeId { get; set; }
    public long? ClassId { get; set; }
    public string? Title { get; set; }
    public string? Status { get; set; }
    public string? Sort { get; set; }
}
