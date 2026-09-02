namespace Domain.Services;

public interface IDevelopmentAuthenticationGuard
{
    bool CanSeed { get; }

    void EnsureEndpointAccess(IReadOnlyCollection<string> suppliedApiKeys);
}
