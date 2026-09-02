using System.Data;

using Domain.Enums;
using Domain.Models;

using Infrastructure.DataAccess;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Shared.DevelopmentAuthentication;

namespace Infrastructure.Seed;

public static class DevelopmentPlayerSeeder
{
    public static async Task<IReadOnlyList<SeededPlayer>> SeedAsync(
        ApplicationDbContext context,
        UserManager<User> userManager,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var results = new List<SeededPlayer>(DevelopmentPlayerCatalog.All.Count);
        foreach (var definition in DevelopmentPlayerCatalog.All)
        {
            var user = await EnsureUserAsync(userManager, definition);
            var player = await EnsurePlayerAsync(context, user, definition, cancellationToken);
            results.Add(new SeededPlayer(definition.AccountKey, user.Id, player.Id));
        }

        await context.SaveChangesAsync(cancellationToken);
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return results;
    }

    private static async Task<User> EnsureUserAsync(
        UserManager<User> userManager,
        DevelopmentPlayerCatalog.Entry definition)
    {
        var byName = await userManager.FindByNameAsync(definition.AccountKey);
        var byEmail = await userManager.FindByEmailAsync(definition.Email);
        if (byName != null && byEmail != null && byName.Id != byEmail.Id)
        {
            throw Incompatible(definition.AccountKey);
        }

        var user = byName ?? byEmail;
        if (user == null)
        {
            user = new User
            {
                UserName = definition.AccountKey,
                Email = definition.Email,
                Name = definition.DisplayName,
                EmailConfirmed = true,
                IsPlatformAdmin = false,
                IsTeacherAccount = false,
                Status = UserStatus.Active,
                LockoutEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Development identity {definition.AccountKey} could not be ensured: {string.Join(" ", created.Errors.Select(x => x.Description))}");
            }

            return user;
        }

        if (!string.IsNullOrWhiteSpace(user.GoogleId)
            || !string.IsNullOrWhiteSpace(user.FirebaseUid)
            || user.IsPlatformAdmin
            || user.IsTeacherAccount
            || !string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw Incompatible(definition.AccountKey);
        }

        user.UserName = definition.AccountKey;
        user.Email = definition.Email;
        user.Name = definition.DisplayName;
        user.EmailConfirmed = true;
        user.Status = UserStatus.Active;
        user.LockoutEnabled = true;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        user.UpdatedAt = DateTime.UtcNow;

        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            throw Incompatible(definition.AccountKey);
        }

        return user;
    }

    private static async Task<Player> EnsurePlayerAsync(
        ApplicationDbContext context,
        User user,
        DevelopmentPlayerCatalog.Entry definition,
        CancellationToken cancellationToken)
    {
        var byUser = await context.Players.SingleOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
        var byEmail = await context.Players.SingleOrDefaultAsync(x => x.Email == definition.Email, cancellationToken);
        if (byUser != null && byEmail != null && byUser.Id != byEmail.Id)
        {
            throw Incompatible(definition.AccountKey);
        }

        if (byUser == null && byEmail != null)
        {
            throw Incompatible(definition.AccountKey);
        }

        var player = byUser;
        if (player == null)
        {
            player = new Player
            {
                UserId = user.Id,
                Email = definition.Email,
                Name = definition.DisplayName,
                Gold = 0,
                Experience = 0,
                Level = 1,
                Rp = 0,
                RankTier = RankTier.Student,
                HighestRankTier = RankTier.Student,
                TotalMatches = 0,
                TotalWins = 0,
                CreatedAt = DateTime.UtcNow
            };
            context.Players.Add(player);
            await context.SaveChangesAsync(cancellationToken);
            return player;
        }

        if (player.UserId != user.Id || !string.IsNullOrWhiteSpace(player.GoogleId))
        {
            throw Incompatible(definition.AccountKey);
        }

        player.Email = definition.Email;
        player.Name = definition.DisplayName;
        player.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return player;
    }

    private static InvalidOperationException Incompatible(string accountKey) =>
        new($"Development identity {accountKey} conflicts with incompatible existing data.");

    public sealed record SeededPlayer(string AccountKey, long UserId, long PlayerProfileId);
}
