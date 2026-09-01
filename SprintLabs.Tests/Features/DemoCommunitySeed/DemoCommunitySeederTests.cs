using Domain.Models;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Seed;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace Compass.Tests.Features.DemoCommunitySeed;

public class DemoCommunitySeederTests
{
    [Fact]
    public async Task SeedAsync_reserves_invitation_capacity_without_reducing_a_higher_existing_limit()
    {
        await using var context = CreateContext();
        var userManager = CreateUserManager(context);

        await DemoCommunitySeeder.SeedAsync(context, userManager);

        var license = await context.CommunityLicenses.SingleAsync();
        license.UsedTeachers.Should().Be(3);
        license.MaxTeachers.Should().Be(10);

        license.MaxTeachers = 12;
        await context.SaveChangesAsync();

        await DemoCommunitySeeder.SeedAsync(context, userManager);

        license = await context.CommunityLicenses.SingleAsync();
        license.UsedTeachers.Should().Be(3);
        license.MaxTeachers.Should().Be(12);
    }

    private static ApplicationDbContext CreateContext()
    {
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static UserManager<User> CreateUserManager(ApplicationDbContext context)
    {
        var store = new UserStore<User, IdentityRole<long>, ApplicationDbContext, long>(context);
        return new UserManager<User>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            new IUserValidator<User>[] { new UserValidator<User>() },
            new IPasswordValidator<User>[] { new PasswordValidator<User>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<User>>.Instance);
    }
}
