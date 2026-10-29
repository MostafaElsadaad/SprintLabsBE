using System.Net;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.PlayerProfiles.Common;

internal static class PlayerSetupPolicy
{
    internal static async Task<Player> LoadAsync(long userId, IUserService users, IPlayerRepository players)
    {
        var user = await users.GetCurrentUser(userId);
        if (user == null) throw Error(HttpStatusCode.NotFound);
        if (user.IsSuspended || user.IsLockedOut) throw Error(HttpStatusCode.Forbidden);
        return await players.GetByUserIdAsync(userId) ?? throw Error(HttpStatusCode.NotFound);
    }

    internal static Task<bool> IsCommunityManagedAsync(Player player, IBaseRepository<StudentLicense> licenses, CancellationToken ct)
    {
        var email = player.Email.Trim().ToLowerInvariant();
        return licenses.AsQueryable().AnyAsync(x => x.Status != StudentLicenseStatus.Revoked &&
            (x.PlayerProfileId == player.Id || x.UserId == player.UserId && player.UserId != null ||
             x.Email.ToLower() == email), ct);
    }

    internal static PlayerSetupResponse Map(Player player, bool communityManaged) => new()
    {
        PlayerProfileId = player.Id, Nickname = player.Name, Grade = player.Grade,
        Curriculum = player.Curriculum, Language = player.Language,
        IsCommunityManaged = communityManaged,
        RequiresSetup = !communityManaged && (string.IsNullOrWhiteSpace(player.Name) ||
            player.Grade is not (>= 7 and <= 12) || string.IsNullOrWhiteSpace(player.Curriculum) ||
            string.IsNullOrWhiteSpace(player.Language))
    };

    internal static GenericException Error(HttpStatusCode code) => new(
        message: code == HttpStatusCode.BadRequest ? ErrorMessage.InvalidInput :
            code == HttpStatusCode.NotFound ? ErrorMessage.NotFound : ErrorMessage.InvalidAccessToken,
        statusCode: code, errorCode: ErrorCode.Failure);
}
