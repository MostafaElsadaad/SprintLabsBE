using System.Net;

using Application.Features.Users.GetCurrentPlayerProfile;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using FluentAssertions;

using Moq;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.B2CPlayerProfileSupport;

public class GetCurrentPlayerProfileQueryHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IPlayerRepository> _playerRepositoryMock = new();

    [Fact]
    public async Task Handle_CurrentUserWithPlayerProfile_ReturnsOnlyCurrentProfileWithoutCommunityData()
    {
        SetupActiveUser();
        _playerRepositoryMock
            .Setup(x => x.GetByUserIdAsync(10))
            .ReturnsAsync(new Player
            {
                Id = 100,
                UserId = 10,
                GoogleId = "google-sub",
                Email = "b2c@example.com",
                Name = "B2C Player",
                AvatarUrl = "avatar.png",
                Age = 10,
                Grade = 5,
                SchoolName = "Optional School",
                Gold = 5,
                Experience = 50,
                Level = 2
            });
        var handler = CreateHandler();

        var result = await handler.Handle(
            new GetCurrentPlayerProfileQuery { UserId = 10 },
            CancellationToken.None);

        result.Id.Should().Be(100);
        result.Email.Should().Be("b2c@example.com");
        result.Name.Should().Be("B2C Player");
        result.Age.Should().Be(10);
        result.Grade.Should().Be(5);
        result.SchoolName.Should().Be("Optional School");

        _playerRepositoryMock.Verify(x => x.GetByUserIdAsync(10), Times.Once);
        _playerRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_SuspendedUser_ThrowsForbidden()
    {
        _userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 10,
                IsSuspended = true,
                Status = "Suspended"
            });
        var handler = CreateHandler();

        var act = async () => await handler.Handle(
            new GetCurrentPlayerProfileQuery { UserId = 10 },
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.Which.Message.Should().Be(ErrorMessage.InvalidAccessToken);
    }

    private GetCurrentPlayerProfileQueryHandler CreateHandler()
    {
        return new GetCurrentPlayerProfileQueryHandler(
            _userServiceMock.Object,
            _playerRepositoryMock.Object);
    }

    private void SetupActiveUser()
    {
        _userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 10,
                Email = "b2c@example.com",
                Name = "B2C Player",
                Status = "Active"
            });
    }
}
