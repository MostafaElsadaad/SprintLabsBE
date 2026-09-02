using Domain.Enums;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Seed;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using Shared.DevelopmentAuthentication;

namespace Compass.Tests.Features.DevelopmentPlayerAuthentication;

public class DevelopmentPlayerSeederTests
{
    [Fact]
    public async Task SeedAsync_FirstAndRepeatedRunsCreateEightStableDistinctPairs()
    {
        await using var context = CreateContext();
        var userManager = CreateUserManager(context);

        var first = await DevelopmentPlayerSeeder.SeedAsync(context, userManager);
        var firstPairs = first.ToDictionary(x => x.AccountKey, x => (x.UserId, x.PlayerProfileId));
        var second = await DevelopmentPlayerSeeder.SeedAsync(context, userManager);
        var third = await DevelopmentPlayerSeeder.SeedAsync(context, userManager);

        first.Should().HaveCount(8);
        second.Should().HaveCount(8);
        third.Should().HaveCount(8);
        (await userManager.Users.CountAsync()).Should().Be(8);
        (await context.Players.CountAsync()).Should().Be(8);
        first.Select(x => x.UserId).Should().OnlyHaveUniqueItems();
        first.Select(x => x.PlayerProfileId).Should().OnlyHaveUniqueItems();
        second.ToDictionary(x => x.AccountKey, x => (x.UserId, x.PlayerProfileId)).Should().BeEquivalentTo(firstPairs);
        third.ToDictionary(x => x.AccountKey, x => (x.UserId, x.PlayerProfileId)).Should().BeEquivalentTo(firstPairs);
    }

    [Fact]
    public async Task SeedAsync_PreservesProgressionOnRepeatedRun()
    {
        await using var context = CreateContext();
        var userManager = CreateUserManager(context);
        await DevelopmentPlayerSeeder.SeedAsync(context, userManager);
        var player = await context.Players.OrderBy(x => x.Id).FirstAsync();
        player.Gold = 11;
        player.Experience = 250;
        player.Level = 3;
        player.Rp = 42;
        player.RankTier = RankTier.Seeker;
        await context.SaveChangesAsync();

        await DevelopmentPlayerSeeder.SeedAsync(context, userManager);

        player = await context.Players.SingleAsync(x => x.Id == player.Id);
        player.Gold.Should().Be(11);
        player.Experience.Should().Be(250);
        player.Level.Should().Be(3);
        player.Rp.Should().Be(42);
        player.RankTier.Should().Be(RankTier.Seeker);
    }

    [Fact]
    public async Task SeedAsync_CompatibleExistingUserGetsMissingPlayerWithoutChangingUserId()
    {
        await using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var definition = DevelopmentPlayerCatalog.All[0];
        var user = new User
        {
            UserName = definition.AccountKey,
            Email = definition.Email,
            Name = definition.DisplayName,
            EmailConfirmed = true,
            Status = UserStatus.Active
        };
        (await userManager.CreateAsync(user)).Succeeded.Should().BeTrue();

        var results = await DevelopmentPlayerSeeder.SeedAsync(context, userManager);

        results.Single(x => x.AccountKey == definition.AccountKey).UserId.Should().Be(user.Id);
        (await context.Players.SingleAsync(x => x.UserId == user.Id)).Name.Should().Be(definition.DisplayName);
    }

    [Fact]
    public async Task SeedAsync_ProviderOwnedCollisionFailsWithoutTakingOverIdentity()
    {
        await using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var definition = DevelopmentPlayerCatalog.All[0];
        var user = new User
        {
            UserName = definition.AccountKey,
            Email = definition.Email,
            Name = "Existing Provider User",
            FirebaseUid = "provider-uid",
            Status = UserStatus.Active
        };
        (await userManager.CreateAsync(user)).Succeeded.Should().BeTrue();

        var action = () => DevelopmentPlayerSeeder.SeedAsync(context, userManager);

        await action.Should().ThrowAsync<InvalidOperationException>();
        (await userManager.FindByIdAsync(user.Id.ToString()))!.FirebaseUid.Should().Be("provider-uid");
        (await context.Players.CountAsync()).Should().Be(0);
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static UserManager<User> CreateUserManager(ApplicationDbContext context)
    {
        var store = new UserStore<User, IdentityRole<long>, ApplicationDbContext, long>(context);
        return new UserManager<User>(
            store,
            Options.Create(new IdentityOptions { User = { RequireUniqueEmail = true } }),
            new PasswordHasher<User>(),
            new IUserValidator<User>[] { new UserValidator<User>() },
            new IPasswordValidator<User>[] { new PasswordValidator<User>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<User>>.Instance);
    }
}
