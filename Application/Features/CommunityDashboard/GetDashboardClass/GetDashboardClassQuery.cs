using MediatR;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.GetDashboardClass;

public class GetDashboardClassQuery : IRequest<ClassDetail>
{
    public long UserId { get; set; }
    public long ClassId { get; set; }
}
