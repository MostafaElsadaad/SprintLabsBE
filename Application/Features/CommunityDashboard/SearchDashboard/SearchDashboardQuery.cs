using MediatR;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.SearchDashboard;

public class SearchDashboardQuery : IRequest<List<SearchItem>>
{
    public long UserId { get; set; }
    public string? Q { get; set; }
}
