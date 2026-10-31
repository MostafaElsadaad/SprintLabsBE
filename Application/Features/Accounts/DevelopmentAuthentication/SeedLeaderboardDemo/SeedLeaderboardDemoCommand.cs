using MediatR;
namespace Application.Features.Accounts.DevelopmentAuthentication.SeedLeaderboardDemo;
public sealed class SeedLeaderboardDemoCommand : IRequest<SeedLeaderboardDemoResponse>
{
    public long AdminUserId { get; set; }
    public long CommunityId { get; set; }
    public long GradeId { get; set; }
    public long BaseClassId { get; set; }
    public IReadOnlyCollection<string> SuppliedApiKeys { get; set; } = Array.Empty<string>();
}