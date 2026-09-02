using Domain.Models;

using Shared.Responses;

namespace Application.Features.Accounts.Common;

public interface IExternalPlayerLoginWorkflow
{
    Task<LoginResponse> CompleteAsync(UserIdentityResponse user, ExternalPlayerLoginContext context, CancellationToken cancellationToken);
    Task<LoginResponse> CompleteExistingAsync(UserIdentityResponse user, Player player, ExternalPlayerLoginContext context, CancellationToken cancellationToken);
}
