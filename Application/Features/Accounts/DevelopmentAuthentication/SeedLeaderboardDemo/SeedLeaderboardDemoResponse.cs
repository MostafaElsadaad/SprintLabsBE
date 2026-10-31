namespace Application.Features.Accounts.DevelopmentAuthentication.SeedLeaderboardDemo;
public sealed class SeedLeaderboardDemoResponse
{
    public string Scenario { get; set; } = "";
    public List<AssignDevelopmentSchool.AssignDevelopmentSchoolResponse> Players { get; set; } = new();
    public List<long> MatchIds { get; set; } = new();
}