using System.Net;

using Application.Features.Accounts.UpdateProfile;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using FluentAssertions;

using Moq;

using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.AccountProfile;

public class UpdateProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_UsesInternalUserIdToResolveOnlyThatPlayersProfile()
    {
        var users = new Mock<IUserService>();
        var players = new Mock<IPlayerRepository>();
        users.Setup(x => x.GetCurrentUser(7)).ReturnsAsync(new UserIdentityResponse { Id = 7 });
        players.Setup(x => x.GetByUserIdAsync(7)).ReturnsAsync(new Player { Id = 9, UserId = 7, GoogleId = "unrelated-google-id", Email = "player@example.com", Name = "Before" });
        players.Setup(x => x.UpdatePlayer(It.IsAny<Player>())).ReturnsAsync((Player player) => player);
        var handler = new UpdateProfileCommandHandler(players.Object, users.Object);

        var result = await handler.Handle(new UpdateProfileCommand { UserId = 7, Name = "After" }, CancellationToken.None);

        result.Data.Name.Should().Be("After");
        players.Verify(x => x.GetByUserIdAsync(7), Times.Once);
        players.Verify(x => x.GetByGoogleIdAsync(It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SuspendedUser_IsForbiddenBeforePlayerLookup()
    {
        var users = new Mock<IUserService>();
        var players = new Mock<IPlayerRepository>();
        users.Setup(x => x.GetCurrentUser(7)).ReturnsAsync(new UserIdentityResponse { Id = 7, IsSuspended = true });
        var handler = new UpdateProfileCommandHandler(players.Object, users.Object);

        var action = () => handler.Handle(new UpdateProfileCommand { UserId = 7, Name = "After" }, CancellationToken.None);

        var error = await action.Should().ThrowAsync<GenericException>();
        error.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        players.Verify(x => x.GetByUserIdAsync(It.IsAny<long>()), Times.Never);
    }
}
