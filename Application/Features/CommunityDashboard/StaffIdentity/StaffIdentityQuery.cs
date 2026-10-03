using MediatR;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.StaffIdentity;

public class StaffIdentityQuery : IRequest<StaffIdentityResponse>
{
    public long UserId { get; set; }
}
