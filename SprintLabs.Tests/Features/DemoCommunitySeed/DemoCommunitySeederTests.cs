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
    public async Task SeedAsync_adds_sample_term_only_when_empty_and_preserves_existing_configuration()
    {
        await using var context = CreateContext();
        var manager = CreateUserManager(context);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await DemoCommunitySeeder.SeedAsync(context, manager);
        var demo = await context.Communities.SingleAsync();
        demo.TermName.Should().Contain("sample calendar");
        demo.TermStartDate.Should().BeOnOrBefore(today);
        demo.TermEndDate.Should().BeOnOrAfter(today);
        demo.TermName = "Custom term"; demo.TermStartDate = new(2026, 8, 20); demo.TermEndDate = new(2027, 1, 18);
        await context.SaveChangesAsync();
        await DemoCommunitySeeder.SeedAsync(context, manager);
        demo.TermName.Should().Be("Custom term");
        demo.TermStartDate.Should().Be(new DateOnly(2026, 8, 20));
        demo.TermEndDate.Should().Be(new DateOnly(2027, 1, 18));
        context.Matches.Should().BeEmpty();
        context.MatchPlayers.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedAsync_reserves_four_student_import_seats_and_preserves_higher_limits()
    {
        await using var context = CreateContext();
        var userManager = CreateUserManager(context);
        await DemoCommunitySeeder.SeedAsync(context, userManager);
        var license = await context.CommunityLicenses.SingleAsync();
        license.UsedStudents.Should().Be(20);
        (license.MaxStudents - license.UsedStudents).Should().BeGreaterThanOrEqualTo(4);
        license.MaxStudents = 40;
        await context.SaveChangesAsync();
        await DemoCommunitySeeder.SeedAsync(context, userManager);
        (await context.CommunityLicenses.SingleAsync()).MaxStudents.Should().Be(40);
    }

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
