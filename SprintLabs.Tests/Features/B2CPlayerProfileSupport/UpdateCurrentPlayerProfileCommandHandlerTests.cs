using System.Net;
using System.Text.Json;

using Application.Features.PlayerProfiles.UpdateCurrentPlayerProfile;

using Domain.Models;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;

using Moq;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.B2CPlayerProfileSupport;

public class UpdateCurrentPlayerProfileCommandHandlerTests
{
    private readonly Mock<IUserService> _userServiceMock = new();

    [Fact]
    public async Task Handle_CurrentUser_UpdatesEditableFieldsOnly()
    {
        await using var context = CreateContext();
        await SeedPlayers(context);
        SetupActiveUser();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            CreateCommand("{\"age\":10,\"grade\":5,\"schoolName\":\"Optional School\"}"),
            CancellationToken.None);

        result.Id.Should().Be(100);
        result.Age.Should().Be(10);
        result.Grade.Should().Be(5);
        result.SchoolName.Should().Be("Optional School");

        var currentUserPlayer = await context.Players.SingleAsync(x => x.Id == 100);
        currentUserPlayer.Age.Should().Be(10);
        currentUserPlayer.Grade.Should().Be(5);
        currentUserPlayer.SchoolName.Should().Be("Optional School");
        currentUserPlayer.UpdatedAt.Should().NotBeNull();

        var otherUserPlayer = await context.Players.SingleAsync(x => x.Id == 200);
        otherUserPlayer.Age.Should().Be(12);
        otherUserPlayer.Grade.Should().Be(7);
        otherUserPlayer.SchoolName.Should().Be("Other School");
    }

    [Fact]
    public async Task Handle_ExplicitNulls_ClearNullableFieldsAndPreserveOmittedFields()
    {
        await using var context = CreateContext();
        await SeedPlayers(context);
        SetupActiveUser();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            CreateCommand("{\"age\":null,\"schoolName\":null}"),
            CancellationToken.None);

        result.Age.Should().BeNull();
        result.SchoolName.Should().BeNull();
        result.Grade.Should().Be(4);

        var stored = await context.Players.SingleAsync(x => x.Id == 100);
        stored.Age.Should().BeNull();
        stored.SchoolName.Should().BeNull();
        stored.Grade.Should().Be(4);
    }

    [Theory]
    [InlineData("{\"age\":0}")]
    [InlineData("{\"age\":121}")]
    [InlineData("{\"grade\":0}")]
    [InlineData("{\"grade\":21}")]
    [InlineData("{\"schoolName\":\"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\"}")]
    public async Task Handle_InvalidProfileValues_ThrowsBadRequest(string json)
    {
        await using var context = CreateContext();
        await SeedPlayers(context);
        SetupActiveUser();
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(CreateCommand(json), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.Message.Should().Be(ErrorMessage.InvalidInput);
    }

    [Fact]
    public async Task Handle_SuspendedUser_ThrowsForbiddenWithoutUpdatingProfile()
    {
        await using var context = CreateContext();
        await SeedPlayers(context);
        _userServiceMock
            .Setup(x => x.GetCurrentUser(10))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 10,
                IsSuspended = true,
                Status = "Suspended"
            });
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(
            CreateCommand("{\"age\":10}"),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var stored = await context.Players.SingleAsync(x => x.Id == 100);
        stored.Age.Should().Be(9);
    }

    [Fact]
    public async Task Handle_MissingPlayerProfile_ThrowsNotFound()
    {
        await using var context = CreateContext();
        SetupActiveUser();
        var handler = CreateHandler(context);

        var act = async () => await handler.Handle(
            CreateCommand("{\"age\":10}"),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<GenericException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Which.Message.Should().Be(ErrorMessage.NotFound);
    }

    private UpdateCurrentPlayerProfileCommandHandler CreateHandler(ApplicationDbContext context)
    {
        return new UpdateCurrentPlayerProfileCommandHandler(
            _userServiceMock.Object,
            new PlayerRepository(context));
    }

    private UpdateCurrentPlayerProfileCommand CreateCommand(string json)
    {
        var request = JsonSerializer.Deserialize<UpdateCurrentPlayerProfileRequest>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        return new UpdateCurrentPlayerProfileCommand
        {
            UserId = 10,
            Age = request.Age,
            Grade = request.Grade,
            SchoolName = request.SchoolName
        };
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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedPlayers(ApplicationDbContext context)
    {
        context.Players.AddRange(
            new Player
            {
                Id = 100,
                UserId = 10,
                GoogleId = "google-sub",
                Email = "b2c@example.com",
                Name = "B2C Player",
                AvatarUrl = "avatar.png",
                Age = 9,
                Grade = 4,
                SchoolName = "Old School",
                Gold = 5,
                Experience = 50,
                Level = 2,
                CreatedAt = DateTime.UtcNow
            },
            new Player
            {
                Id = 200,
                UserId = 11,
                GoogleId = "other-sub",
                Email = "other@example.com",
                Name = "Other Player",
                Age = 12,
                Grade = 7,
                SchoolName = "Other School",
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();
    }
}
