using MediatR;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.StaffDashboard;

public class StaffDashboardQuery : IRequest<StaffDashboardResponse>
{
    public long UserId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
