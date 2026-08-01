using Shared.Responses;

namespace Application.Features.Accounts.Common;

public interface IExternalPlayerLoginWorkflow
{
    Task<LoginResponse> CompleteAsync(UserIdentityResponse user, ExternalPlayerLoginContext context, CancellationToken cancellationToken);
}
