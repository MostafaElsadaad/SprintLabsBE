using Domain.Enums;

using Infrastructure.DataAccess;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Seed;

public static class PlatformAdminSeeder
{
    public static async Task SeedAsync(
        UserManager<User> userManager,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var username = configuration["PlatformAdminSeed:Username"]?.Trim();
        var email = configuration["PlatformAdminSeed:Email"]?.Trim();
        var password = configuration["PlatformAdminSeed:Password"];
        var name = configuration["PlatformAdminSeed:Name"]?.Trim() ?? "Platform Administrator";

        if (string.IsNullOrWhiteSpace(username) &&
            string.IsNullOrWhiteSpace(email) &&
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Platform admin seed configuration requires username, email, and password.");
        }

        var byUsername = await userManager.FindByNameAsync(username);
        var byEmail = await userManager.FindByEmailAsync(email);
        if (byUsername != null && byEmail != null && byUsername.Id != byEmail.Id)
        {
            throw new InvalidOperationException("Platform admin seed username and email identify different users.");
        }

        var existingUser = byUsername ?? byEmail;
        if (existingUser != null)
        {
            if (!existingUser.IsPlatformAdmin)
            {
                throw new InvalidOperationException("Platform admin seed identity already belongs to a non-admin user.");
            }

            return;
        }

        var user = new User
        {
            UserName = username,
            Email = email,
            Name = name,
            IsPlatformAdmin = true,
            IsTeacherAccount = false,
            EmailConfirmed = true,
            LockoutEnabled = true,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Platform admin seed failed: {string.Join(" ", result.Errors.Select(x => x.Description))}");
        }
    }
}
