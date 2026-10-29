using Application.Features.PlayerProfiles.Common;
using MediatR;
namespace Application.Features.PlayerProfiles.GetPlayerSetup;

public class GetPlayerSetupQuery : IRequest<PlayerSetupResponse>
{
    public long UserId { get; set; }
}
