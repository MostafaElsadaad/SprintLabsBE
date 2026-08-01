using Shared.Responses;

namespace Domain.Services;

public interface IFirebaseAuthenticationService
{
    Task<FirebaseUserResponse> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken);
}
