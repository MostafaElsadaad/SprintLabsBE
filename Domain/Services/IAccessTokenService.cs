using Shared.Responses;

namespace Domain.Services;

public interface IAccessTokenService
{
    AccessTokenResult Create(long userId, string email, string name);
}
