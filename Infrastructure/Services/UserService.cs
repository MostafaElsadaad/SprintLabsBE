using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

using Domain.Enums;
using Domain.Services;

using Infrastructure.DataAccess;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Shared.Enums;
using Shared.Exceptions;
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
            _jwtSettings = config.GetSection("JWTOptions").Get<JWTOptions>()
                ?? throw new InvalidOperationException("JWTOptions are not configured.");
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
                   .AnyAsync(x => x.Email != null && x.Email.ToLower() == email.ToLower());

        }

        public async Task<UserIdentityResponse> FindOrCreateGoogleUser(GoogleUserResponse response)
        {
            var email = response.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidInput,
                    statusCode: HttpStatusCode.BadRequest,
                    errorCode: ErrorCode.Failure);
            }

            var normalizedEmail = _userManager.NormalizeEmail(email);
            var user = await _userManager.Users
                .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail || (x.Email != null && x.Email.ToLower() == email.ToLower()));

            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    NormalizedUserName = _userManager.NormalizeName(email),
                    Email = email,
                    NormalizedEmail = normalizedEmail,
                    GoogleId = response.Sub,
                    Name = string.IsNullOrWhiteSpace(response.Name) ? email : response.Name,
                    AvatarUrl = response.Picture,
                    IsPlatformAdmin = false,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                var createdUser = await _userManager.CreateAsync(user);
                if (!createdUser.Succeeded)
                {
                    throw new GenericException(
                        message: ErrorMessage.UserCreationFailed,
                        statusCode: HttpStatusCode.BadRequest,
                        errorCode: ErrorCode.Failure);
                }
            }
            else
            {
                var hasChanges = false;

                if (!string.IsNullOrWhiteSpace(response.Sub) && user.GoogleId != response.Sub)
                {
                    user.GoogleId = response.Sub;
                    hasChanges = true;
                }

                var name = string.IsNullOrWhiteSpace(response.Name) ? email : response.Name;
                if (user.Name != name)
                {
                    user.Name = name;
                    hasChanges = true;
                }

                if (user.AvatarUrl != response.Picture)
                {
                    user.AvatarUrl = response.Picture;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                    var updatedUser = await _userManager.UpdateAsync(user);
                    if (!updatedUser.Succeeded)
                    {
                        throw new GenericException(
                            message: ErrorMessage.NotModified,
                            statusCode: HttpStatusCode.BadRequest,
                            errorCode: ErrorCode.Failure);
                    }
                }
            }

            return new UserIdentityResponse
            {
                Id = user.Id,
                GoogleId = user.GoogleId,
                Email = user.Email ?? email,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status.ToString(),
                IsPlatformAdmin = user.IsPlatformAdmin,
                IsSuspended = user.Status == UserStatus.Suspended,
                HasGoogleIdentity = !string.IsNullOrWhiteSpace(user.GoogleId),
                PlayerProfileId = user.Player?.Id
            };
        }

        public async Task<UserIdentityResponse> FindOrCreateBasicUser(string email, string name)
        {
            email = email.Trim();
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name))
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidInput,
                    statusCode: HttpStatusCode.BadRequest,
                    errorCode: ErrorCode.Failure);
            }

            var normalizedEmail = _userManager.NormalizeEmail(email);
            var user = await _userManager.Users
                .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail || (x.Email != null && x.Email.ToLower() == email.ToLower()));

            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    NormalizedUserName = _userManager.NormalizeName(email),
                    Email = email,
                    NormalizedEmail = normalizedEmail,
                    Name = name,
                    IsPlatformAdmin = false,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                var createdUser = await _userManager.CreateAsync(user);
                if (!createdUser.Succeeded)
                {
                    throw new GenericException(
                        message: ErrorMessage.UserCreationFailed,
                        statusCode: HttpStatusCode.BadRequest,
                        errorCode: ErrorCode.Failure);
                }
            }
            return new UserIdentityResponse
            {
                Id = user.Id,
                GoogleId = user.GoogleId,
                Email = user.Email ?? email,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status.ToString(),
                IsPlatformAdmin = user.IsPlatformAdmin,
                IsSuspended = user.Status == UserStatus.Suspended,
                HasGoogleIdentity = !string.IsNullOrWhiteSpace(user.GoogleId),
                PlayerProfileId = user.Player?.Id
            };
        }

        public async Task<UserIdentityResponse?> FindByEmail(string email)
        {
            email = email.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var normalizedEmail = _userManager.NormalizeEmail(email);
            var user = await _userManager.Users
                .Include(x => x.Player)
                .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail || (x.Email != null && x.Email.ToLower() == email.ToLower()));

            if (user == null)
            {
                return null;
            }

            return new UserIdentityResponse
            {
                Id = user.Id,
                GoogleId = user.GoogleId,
                Email = user.Email ?? email,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status.ToString(),
                IsPlatformAdmin = user.IsPlatformAdmin,
                IsSuspended = user.Status == UserStatus.Suspended,
                HasGoogleIdentity = !string.IsNullOrWhiteSpace(user.GoogleId),
                PlayerProfileId = user.Player?.Id
            };
        }

        public async Task<List<long>> SearchUserIds(string search, CancellationToken cancellationToken)
        {
            search = search.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(search))
            {
                return new List<long>();
            }

            return await _userManager.Users
                .Where(x =>
                    (x.Email != null && x.Email.ToLower().Contains(search))
                    || x.Name.ToLower().Contains(search))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<UserIdentityResponse>> GetUsersByIds(
            IEnumerable<long> userIds,
            CancellationToken cancellationToken)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new List<UserIdentityResponse>();
            }

            return await _userManager.Users
                .Where(x => ids.Contains(x.Id))
                .Select(x => new UserIdentityResponse
                {
                    Id = x.Id,
                    GoogleId = x.GoogleId,
                    Email = x.Email ?? string.Empty,
                    Name = x.Name,
                    AvatarUrl = x.AvatarUrl,
                    Status = x.Status.ToString(),
                    IsPlatformAdmin = x.IsPlatformAdmin,
                    IsSuspended = x.Status == UserStatus.Suspended,
                    HasGoogleIdentity = !string.IsNullOrWhiteSpace(x.GoogleId),
                    PlayerProfileId = x.Player != null ? x.Player.Id : null
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<UserIdentityResponse?> GetCurrentUser(long userId)
        {
            var user = await _userManager.Users
                .Include(x => x.Player)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
            {
                return null;
            }

            return new UserIdentityResponse
            {
                Id = user.Id,
                GoogleId = user.GoogleId,
                Email = user.Email ?? string.Empty,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status.ToString(),
                IsPlatformAdmin = user.IsPlatformAdmin,
                IsSuspended = user.Status == UserStatus.Suspended,
                HasGoogleIdentity = !string.IsNullOrWhiteSpace(user.GoogleId),
                PlayerProfileId = user.Player?.Id
            };
        }

    }
}
