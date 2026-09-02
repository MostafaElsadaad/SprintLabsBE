using System.Net;

using Application.Features.Accounts.DevelopmentAuthentication.ListDevelopmentPlayers;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using FluentAssertions;

using Moq;

using Shared.DevelopmentAuthentication;
using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.DevelopmentPlayerAuthentication;

public class ListDevelopmentPlayersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsExactlyEightSafeOrderedEntries()
    {
        var fixture = CreateValidFixture();

        var result = await fixture.Handler.Handle(new ListDevelopmentPlayersQuery(), CancellationToken.None);

        result.Should().HaveCount(8);
        result.Select(x => x.AccountKey).Should().Equal(DevelopmentPlayerCatalog.All.Select(x => x.AccountKey));
        result.Select(x => x.DisplayName).Should().Equal(DevelopmentPlayerCatalog.All.Select(x => x.DisplayName));
        typeof(DevelopmentPlayerResponse).GetProperties().Select(x => x.Name)
            .Should().BeEquivalentTo(new[] { "AccountKey", "DisplayName" });
    }

    [Fact]
    public async Task Handle_MissingPairFailsWholeRequestWithoutCreatingAnything()
    {
        var fixture = CreateValidFixture();
        fixture.Users.Setup(x => x.FindByEmail(DevelopmentPlayerCatalog.All[3].Email))
            .ReturnsAsync((UserIdentityResponse?)null);

        var action = () => fixture.Handler.Handle(new ListDevelopmentPlayersQuery(), CancellationToken.None);

        var error = await action.Should().ThrowAsync<GenericException>();
        error.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        fixture.Players.Verify(x => x.CreateAsync(It.IsAny<Player>()), Times.Never);
        fixture.Players.Verify(x => x.UpdatePlayer(It.IsAny<Player>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvokesGuardBeforeReadingAccounts()
    {
        var fixture = CreateValidFixture();
        fixture.Guard.Setup(x => x.EnsureEndpointAccess(It.IsAny<IReadOnlyCollection<string>>()))
            .Throws(new GenericException(Shared.Enums.ErrorCode.Failure, Shared.Enums.ErrorMessage.NotFound, HttpStatusCode.NotFound));

        var action = () => fixture.Handler.Handle(new ListDevelopmentPlayersQuery { SuppliedApiKeys = ["bad"] }, CancellationToken.None);

        await action.Should().ThrowAsync<GenericException>();
        fixture.Users.Verify(x => x.FindByEmail(It.IsAny<string>()), Times.Never);
    }

    private static Fixture CreateValidFixture()
    {
        var guard = new Mock<IDevelopmentAuthenticationGuard>();
        var users = new Mock<IUserService>();
        var players = new Mock<IPlayerRepository>();
        long id = 1;
        foreach (var definition in DevelopmentPlayerCatalog.All)
        {
            var userId = id++;
            var playerId = id++;
            users.Setup(x => x.FindByEmail(definition.Email)).ReturnsAsync(new UserIdentityResponse
            {
                Id = userId,
                UserName = definition.AccountKey,
                Email = definition.Email,
                Name = definition.DisplayName,
                PlayerProfileId = playerId
            });
            players.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(new Player
            {
                Id = playerId,
                UserId = userId,
                Email = definition.Email,
                Name = definition.DisplayName
            });
        }

        return new Fixture(
            guard,
            users,
            players,
            new ListDevelopmentPlayersQueryHandler(guard.Object, users.Object, players.Object));
    }

    private sealed record Fixture(
        Mock<IDevelopmentAuthenticationGuard> Guard,
        Mock<IUserService> Users,
        Mock<IPlayerRepository> Players,
        ListDevelopmentPlayersQueryHandler Handler);
}
