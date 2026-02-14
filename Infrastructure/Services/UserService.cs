using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Domain.Services;

using Infrastructure.DataAccess;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Shared.Options;
using Shared.Responses;

namespace Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;
        private readonly JWTOptions _jwtSettings;
        private readonly UserManager<User> _userManager;


        public UserService(IConfiguration config, UserManager<User> userManager)
        {
            _config = config;
            _jwtSettings = config.GetSection("JWTOptions").Get<JWTOptions>();
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            _userManager = userManager;
        }
        public async Task<LoginResponse> Authenticate(List<Claim> claims)
        {
            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(7),
                SigningCredentials = creds,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new LoginResponse
            {
                AccessToken = tokenHandler.WriteToken(token)
            };
        }

        public async Task<long> CreateAccount(GoogleUserResponse response, int userProfileId)
        {
            var appUser = new User
            {
                UserName = response.Email,
                Email = response.Email,
                UserProfileId = userProfileId
            };

            var createdUser = await _userManager.CreateAsync(appUser);

            return appUser.Id;
        }

        public async Task<bool> EnsureUserExists(string email)
        {
            return await _userManager.Users
                   .AnyAsync(x => x.Email.ToLower() == email.ToLower());

        }

    }
}
