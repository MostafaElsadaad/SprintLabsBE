namespace Shared.Options;

public sealed class DevelopmentAuthenticationOptions
{
    public const string SectionName = "DevelopmentAuthentication";
    public const string HeaderName = "X-SprintLabs-Dev-Key";

    public bool Enabled { get; set; }
    public bool SeedPlayers { get; set; }
    public string? ApiKey { get; set; }
}
