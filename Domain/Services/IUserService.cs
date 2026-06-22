using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using Shared.Responses;

namespace Domain.Services
{
    public interface IUserService
    {
        public Task<LoginResponse> Authenticate(List<Claim> claims);
        public Task<bool> EnsureUserExists(string email);
        public Task<long> CreateAccount(GoogleUserResponse response, int userProfileId);
        public Task<UserIdentityResponse> FindOrCreateGoogleUser(GoogleUserResponse response);
        public Task<UserIdentityResponse> FindOrCreateBasicUser(string email, string name);
        public Task<UserIdentityResponse?> GetCurrentUser(long userId);

    }
}
