using Application.Features.PlayerProfiles.Common;
using MediatR;
namespace Application.Features.PlayerProfiles.SavePlayerSetup;

public class SavePlayerSetupCommand : IRequest<PlayerSetupResponse>
{
    public long UserId { get; set; }
    public string? Nickname { get; set; }
    public int Grade { get; set; }
    public string? Curriculum { get; set; }
    public string? Language { get; set; }
}
