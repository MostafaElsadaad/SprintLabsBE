using MediatR;
namespace Application.Features.Accounts.DevelopmentAuthentication.SeedJourneyDemo;
public sealed class SeedJourneyDemoCommand : IRequest<SeedJourneyDemoResponse>
{
    public long AdminUserId { get; set; }
    public IReadOnlyCollection<string> SuppliedApiKeys { get; set; } = Array.Empty<string>();
}
