using Shared.Enums;
using Shared.Responses;

namespace Domain.Services;

public interface IAccessTokenService
{
    AccessTokenResult Create(long userId, string email, string name);
    AccessTokenResult Create(long userId, string email, string name, AuthenticatedAccountType accountType);
}
