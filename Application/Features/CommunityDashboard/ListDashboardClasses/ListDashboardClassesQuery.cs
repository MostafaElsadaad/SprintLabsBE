using MediatR;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.ListDashboardClasses;

public class ListDashboardClassesQuery : IRequest<PagedResponse<ClassView>>
{
    public long UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public long? GradeId { get; set; }
    public long? TeacherId { get; set; }
    public string? Status { get; set; }
    public string? Sort { get; set; }
}
