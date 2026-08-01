using System.Net;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.FirebasePlayerAuthentication;

public class FirebaseUserIdentityResolutionTests
{
    [Fact]
    public async Task FindOrCreateFirebaseUser_FirstAndRepeatedLogin_ReturnsOneUserWithFirebaseUid()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var firebaseUser = FirebaseUser("firebase-uid", "player@example.com");

        var first = await service.FindOrCreateFirebaseUser(firebaseUser, CancellationToken.None);
        var repeated = await service.FindOrCreateFirebaseUser(firebaseUser, CancellationToken.None);

        first.Id.Should().Be(repeated.Id);
        (await context.Users.CountAsync()).Should().Be(1);
        (await context.Users.SingleAsync()).FirebaseUid.Should().Be("firebase-uid");
    }

    [Fact]
    public async Task FindOrCreateFirebaseUser_VerifiedGoogleIdentity_LinksExistingLegacyUserWithoutChangingGoogleId()
    {
        await using var context = CreateContext();
        context.Users.Add(User(7, "legacy@example.com", "google-legacy"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.FindOrCreateFirebaseUser(FirebaseUser("firebase-uid", "legacy@example.com", "google-legacy"), CancellationToken.None);

        result.Id.Should().Be(7);
        var linked = await context.Users.SingleAsync();
        linked.GoogleId.Should().Be("google-legacy");
        linked.FirebaseUid.Should().Be("firebase-uid");
    }

    [Fact]
    public async Task FindOrCreateFirebaseUser_ConflictingTrustedCandidates_ReturnsConflictWithoutMutation()
    {
        await using var context = CreateContext();
        context.Users.AddRange(User(1, "one@example.com", "google-one", "firebase-one"), User(2, "two@example.com", "google-two"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var action = () => service.FindOrCreateFirebaseUser(FirebaseUser("firebase-one", "two@example.com", "google-two"), CancellationToken.None);

        var error = await action.Should().ThrowAsync<GenericException>();
        error.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await context.Users.SingleAsync(x => x.Id == 2)).FirebaseUid.Should().BeNull();
    }

    [Fact]
    public async Task FindOrCreateFirebaseUser_UnverifiedEmailCannotClaimExistingUser()
    {
        await using var context = CreateContext();
        context.Users.Add(User(1, "legacy@example.com", "google-legacy"));
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var firebaseUser = FirebaseUser("firebase-new", "legacy@example.com");
        firebaseUser.EmailVerified = false;

        var action = () => service.FindOrCreateFirebaseUser(firebaseUser, CancellationToken.None);

        var error = await action.Should().ThrowAsync<GenericException>();
        error.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await context.Users.SingleAsync()).FirebaseUid.Should().BeNull();
    }

    private static FirebaseUserResponse FirebaseUser(string uid, string email, string? googleId = null) => new()
    {
        Uid = uid,
        Email = email,
        EmailVerified = true,
        Name = "Player",
        GoogleProviderId = googleId
    };

    private static User User(long id, string email, string? googleId = null, string? firebaseUid = null) => new()
    {
        Id = id,
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        SecurityStamp = Guid.NewGuid().ToString(),
        ConcurrencyStamp = Guid.NewGuid().ToString(),
        Name = "Legacy",
        GoogleId = googleId,
        FirebaseUid = firebaseUid
    };

    private static UserService CreateService(ApplicationDbContext context)
    {
        var store = new UserStore<User, IdentityRole<long>, ApplicationDbContext, long>(context);
        var manager = new UserManager<User>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            new IUserValidator<User>[] { new UserValidator<User>() },
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            NullLogger<UserManager<User>>.Instance);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JWTOptions:Issuer"] = "issuer",
            ["JWTOptions:Audience"] = "audience",
            ["JWTOptions:Secret"] = "test-secret-that-is-long-enough-for-hmac-signing"
        }).Build();
        return new UserService(configuration, manager);
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);
}
