using System.Net;

using Application.Features.Accounts.Common;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using FluentAssertions;

using Moq;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.FirebasePlayerAuthentication;

public class ExternalPlayerLoginWorkflowTests
{
    [Fact]
    public async Task CompleteAsync_NewFirebaseB2CPlayer_PreservesProgressionDefaultsAndDoesNotUseGoogleId()
    {
        var userService = new Mock<IUserService>();
        var players = new Mock<IPlayerRepository>();
        var activation = new Mock<ICommunityLoginActivationService>();
        players.Setup(x => x.GetByUserIdAsync(1)).ReturnsAsync((Player?)null);
        players.Setup(x => x.GetByGoogleIdAsync(null)).ReturnsAsync((Player?)null);
        players.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Player?)null);
        players.Setup(x => x.CreateAsync(It.IsAny<Player>())).ReturnsAsync((Player player) => { player.Id = 2; return player; });
        userService.Setup(x => x.Authenticate(It.IsAny<List<System.Security.Claims.Claim>>()))
            .ReturnsAsync(new LoginResponse { AccessToken = "jwt" });
        var workflow = new ExternalPlayerLoginWorkflow(userService.Object, players.Object, activation.Object);

        var result = await workflow.CompleteAsync(new UserIdentityResponse { Id = 1 }, FirebaseContext(), CancellationToken.None);

        result.UserId.Should().Be(1);
        result.PlayerProfileId.Should().Be(2);
        result.Gold.Should().Be(0);
        result.Experience.Should().Be(0);
        result.Level.Should().Be(1);
        players.Verify(x => x.CreateAsync(It.Is<Player>(p => p.UserId == 1 && p.GoogleId == null && p.Gold == 0 && p.Experience == 0 && p.Level == 1)), Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_RepeatedLogin_ReusesPlayerAndPreservesProgression()
    {
        var userService = new Mock<IUserService>();
        var players = new Mock<IPlayerRepository>();
        var activation = new Mock<ICommunityLoginActivationService>();
        players.Setup(x => x.GetByUserIdAsync(1)).ReturnsAsync(new Player { Id = 2, UserId = 1, Email = "player@example.com", Name = "Player", Gold = 17, Experience = 88, Level = 4 });
        userService.Setup(x => x.Authenticate(It.IsAny<List<System.Security.Claims.Claim>>())).ReturnsAsync(new LoginResponse { AccessToken = "jwt" });

        var result = await new ExternalPlayerLoginWorkflow(userService.Object, players.Object, activation.Object)
            .CompleteAsync(new UserIdentityResponse { Id = 1 }, FirebaseContext(), CancellationToken.None);

        result.Gold.Should().Be(17);
        result.Experience.Should().Be(88);
        result.Level.Should().Be(4);
        players.Verify(x => x.CreateAsync(It.IsAny<Player>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_SuspendedUser_IsForbiddenBeforePlayerOrActivationChanges()
    {
        var players = new Mock<IPlayerRepository>();
        var workflow = new ExternalPlayerLoginWorkflow(new Mock<IUserService>().Object, players.Object, new Mock<ICommunityLoginActivationService>().Object);

        var action = () => workflow.CompleteAsync(new UserIdentityResponse { Id = 1, IsSuspended = true }, FirebaseContext(), CancellationToken.None);

        var error = await action.Should().ThrowAsync<GenericException>();
        error.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        players.Verify(x => x.GetByUserIdAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_FirebaseOnlyActivatesTeacherAndStudentUsingVerifiedEmail()
    {
        var userService = new Mock<IUserService>();
        var players = new Mock<IPlayerRepository>();
        var activation = new Mock<ICommunityLoginActivationService>();
        players.Setup(x => x.GetByUserIdAsync(1)).ReturnsAsync(new Player { Id = 2, UserId = 1, Email = "player@example.com", Name = "Player" });
        userService.Setup(x => x.Authenticate(It.IsAny<List<System.Security.Claims.Claim>>())).ReturnsAsync(new LoginResponse());

        await new ExternalPlayerLoginWorkflow(userService.Object, players.Object, activation.Object)
            .CompleteAsync(new UserIdentityResponse { Id = 1 }, FirebaseContext(), CancellationToken.None);

        activation.Verify(x => x.ActivatePendingStudentLicensesAsync(1, 2, "player@example.com", It.IsAny<CancellationToken>()), Times.Once);
        activation.Verify(x => x.ActivateEligiblePendingTeacherMembershipsAsync(1, "player@example.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_LegacyPlayerOwnedByAnotherUser_ReturnsConflict()
    {
        var players = new Mock<IPlayerRepository>();
        players.Setup(x => x.GetByUserIdAsync(1)).ReturnsAsync((Player?)null);
        players.Setup(x => x.GetByGoogleIdAsync("google-id")).ReturnsAsync(new Player { Id = 2, UserId = 3, GoogleId = "google-id", Email = "player@example.com", Name = "Player" });
        var context = FirebaseContext();
        context.GoogleProviderId = "google-id";
        var workflow = new ExternalPlayerLoginWorkflow(new Mock<IUserService>().Object, players.Object, new Mock<ICommunityLoginActivationService>().Object);

        var action = () => workflow.CompleteAsync(new UserIdentityResponse { Id = 1 }, context, CancellationToken.None);

        var error = await action.Should().ThrowAsync<GenericException>();
        error.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private static ExternalPlayerLoginContext FirebaseContext() => new()
    {
        Subject = "firebase-uid",
        Email = "player@example.com",
        EmailVerified = true,
        Name = "Player",
        PictureUrl = "picture.png",
        ActivatePendingTeacherMemberships = true
    };
}
