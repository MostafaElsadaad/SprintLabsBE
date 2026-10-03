using MediatR;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.ListDashboardStudents;

public class ListDashboardStudentsQuery : IRequest<PagedResponse<StudentView>>
{
    public long UserId { get; set; }
    public long? ClassId { get; set; }
    public long? GradeId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Sort { get; set; }
}
