using System.Net;

using Application.Features.Accounts.Common;
using Application.Features.Accounts.DevelopmentAuthentication.DevelopmentLogin;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using FluentAssertions;

using Moq;

using Shared.DevelopmentAuthentication;
using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.DevelopmentPlayerAuthentication;

public class DevelopmentLoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidSeededPairReturnsExistingLoginResponseAndNeverCreatesPlayer()
    {
        var fixture = CreateValidFixture();
        fixture.Workflow.Setup(x => x.CompleteExistingAsync(
                fixture.User,
                fixture.Player,
                It.Is<ExternalPlayerLoginContext>(context => context.Subject == fixture.Definition.AccountKey),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse { AccessToken = "jwt", UserId = fixture.User.Id, PlayerProfileId = fixture.Player.Id });

        var result = await fixture.Handler.Handle(
            new DevelopmentLoginCommand { AccountKey = fixture.Definition.AccountKey },
            CancellationToken.None);

        result.AccessToken.Should().Be("jwt");
        result.UserId.Should().Be(fixture.User.Id);
        result.PlayerProfileId.Should().Be(fixture.Player.Id);
        fixture.Players.Verify(x => x.CreateAsync(It.IsAny<Player>()), Times.Never);
        fixture.Players.Verify(x => x.UpdatePlayer(It.IsAny<Player>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" DEV-PLAYER-01")]
    [InlineData("DEV-PLAYER-01")]
    public async Task Handle_NonCanonicalKeyReturnsBadRequest(string accountKey)
    {
        var fixture = CreateValidFixture();

        var action = () => fixture.Handler.Handle(new DevelopmentLoginCommand { AccountKey = accountKey }, CancellationToken.None);

        var error = await action.Should().ThrowAsync<GenericException>();
        error.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        fixture.Users.Verify(x => x.FindByEmail(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownCanonicalKeyReturnsNotFoundWithoutLookupOrCreation()
    {
        var fixture = CreateValidFixture();

        var action = () => fixture.Handler.Handle(new DevelopmentLoginCommand { AccountKey = "dev-player-09" }, CancellationToken.None);

        var error = await action.Should().ThrowAsync<GenericException>();
        error.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        fixture.Users.Verify(x => x.FindByEmail(It.IsAny<string>()), Times.Never);
        fixture.Players.Verify(x => x.CreateAsync(It.IsAny<Player>()), Times.Never);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Handle_SuspendedOrLockedUserReturnsForbidden(bool suspended, bool locked)
    {
        var fixture = CreateValidFixture();
        fixture.User.IsSuspended = suspended;
        fixture.User.IsLockedOut = locked;

        var action = () => fixture.Handler.Handle(new DevelopmentLoginCommand { AccountKey = fixture.Definition.AccountKey }, CancellationToken.None);

        var error = await action.Should().ThrowAsync<GenericException>();
        error.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        fixture.Workflow.Verify(x => x.CompleteExistingAsync(It.IsAny<UserIdentityResponse>(), It.IsAny<Player>(), It.IsAny<ExternalPlayerLoginContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MissingPlayerReturnsConflictWithoutRepair()
    {
        var fixture = CreateValidFixture();
        fixture.Players.Setup(x => x.GetByUserIdAsync(fixture.User.Id)).ReturnsAsync((Player?)null);

        var action = () => fixture.Handler.Handle(new DevelopmentLoginCommand { AccountKey = fixture.Definition.AccountKey }, CancellationToken.None);

        var error = await action.Should().ThrowAsync<GenericException>();
        error.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        fixture.Players.Verify(x => x.CreateAsync(It.IsAny<Player>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvokesGuardBeforeAccountLookup()
    {
        var fixture = CreateValidFixture();
        fixture.Guard.Setup(x => x.EnsureEndpointAccess(It.IsAny<IReadOnlyCollection<string>>()))
            .Throws(new GenericException(Shared.Enums.ErrorCode.Failure, Shared.Enums.ErrorMessage.NotFound, HttpStatusCode.NotFound));

        var action = () => fixture.Handler.Handle(
            new DevelopmentLoginCommand { AccountKey = fixture.Definition.AccountKey, SuppliedApiKeys = ["bad"] },
            CancellationToken.None);

        await action.Should().ThrowAsync<GenericException>();
        fixture.Users.Verify(x => x.FindByEmail(It.IsAny<string>()), Times.Never);
    }

    private static Fixture CreateValidFixture()
    {
        var definition = DevelopmentPlayerCatalog.All[0];
        var user = new UserIdentityResponse
        {
            Id = 101,
            UserName = definition.AccountKey,
            Email = definition.Email,
            Name = definition.DisplayName,
            PlayerProfileId = 201
        };
        var player = new Player { Id = 201, UserId = 101, Email = definition.Email, Name = definition.DisplayName };
        var guard = new Mock<IDevelopmentAuthenticationGuard>();
        var users = new Mock<IUserService>();
        var players = new Mock<IPlayerRepository>();
        var workflow = new Mock<IExternalPlayerLoginWorkflow>();
        users.Setup(x => x.FindByEmail(definition.Email)).ReturnsAsync(user);
        players.Setup(x => x.GetByUserIdAsync(user.Id)).ReturnsAsync(player);
        var handler = new DevelopmentLoginCommandHandler(guard.Object, users.Object, players.Object, workflow.Object);
        return new Fixture(definition, user, player, guard, users, players, workflow, handler);
    }

    private sealed record Fixture(
        DevelopmentPlayerCatalog.Entry Definition,
        UserIdentityResponse User,
        Player Player,
        Mock<IDevelopmentAuthenticationGuard> Guard,
        Mock<IUserService> Users,
        Mock<IPlayerRepository> Players,
        Mock<IExternalPlayerLoginWorkflow> Workflow,
        DevelopmentLoginCommandHandler Handler);
}
