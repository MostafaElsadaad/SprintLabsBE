using MediatR;
using Shared.Requests;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.WriteDashboardClass;

public class WriteDashboardClassCommand : IRequest<ClassDetail>
{
    public long UserId { get; set; }
    public long? ClassId { get; set; }
    public DashboardClassRequest Body { get; set; } = new();
}
