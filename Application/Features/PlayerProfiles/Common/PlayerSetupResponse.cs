namespace Application.Features.PlayerProfiles.Common;

public class PlayerSetupResponse
{
    public long PlayerProfileId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public int? Grade { get; set; }
    public string? Curriculum { get; set; }
    public string? Language { get; set; }
    public bool IsCommunityManaged { get; set; }
    public bool RequiresSetup { get; set; }
}
