using System.Security.Claims;

using Application.Features.Accounts.GoogleAuthenticate;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

using Moq;

using Shared.Responses;

namespace Compass.Tests.Features.B2CPlayerProfileSupport;

public class B2CLoginCompatibilityTests
{
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IGoogleAuthenticationService> _googleAuthenticationServiceMock = new();

    [Fact]
    public async Task Handle_NoCommunityUserOrStudentLicense_CreatesPlayerAndReturnsLogin()
    {
        await using var context = CreateContext();
        SetupGoogleLogin();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GoogleAuthenticationCommand { IdToken = "google-token" },
            CancellationToken.None);

        result.AccessToken.Should().Be("jwt-token");
        result.UserId.Should().Be(20);
        result.PlayerProfileId.Should().NotBeNull();
        result.Email.Should().Be("b2c@example.com");
        result.Name.Should().Be("B2C Player");
        result.PictureUrl.Should().Be("avatar.png");
        result.Gold.Should().Be(0);
        result.Experience.Should().Be(0);
        result.Level.Should().Be(1);

        var player = await context.Players.SingleAsync();
        player.UserId.Should().Be(20);
        player.GoogleId.Should().Be("google-sub");
        player.Email.Should().Be("b2c@example.com");

        (await context.CommunityUsers.CountAsync()).Should().Be(0);
        (await context.StudentLicenses.CountAsync()).Should().Be(0);
    }

    private GoogleAuthenticationCommandHandler CreateHandler(ApplicationDbContext context)
    {
        var activationService = new CommunityLoginActivationService(
            new BaseRepository<CommunityUser>(context),
            new BaseRepository<StudentLicense>(context));

        return new GoogleAuthenticationCommandHandler(
            _userServiceMock.Object,
            _googleAuthenticationServiceMock.Object,
            new PlayerRepository(context),
            activationService);
    }

    private void SetupGoogleLogin()
    {
        _googleAuthenticationServiceMock
            .Setup(x => x.GetUserInfo("google-token"))
            .ReturnsAsync(new GoogleUserResponse
            {
                Sub = "google-sub",
                Email = "b2c@example.com",
                Name = "B2C Player",
                Picture = "avatar.png"
            });

        _googleAuthenticationServiceMock
            .Setup(x => x.GenerateGoogleClaims(It.IsAny<GoogleUserResponse>()))
            .Returns(new List<Claim>());

        _userServiceMock
            .Setup(x => x.FindOrCreateGoogleUser(It.IsAny<GoogleUserResponse>()))
            .ReturnsAsync(new UserIdentityResponse
            {
                Id = 20,
                Email = "b2c@example.com",
                Name = "B2C Player",
                Status = "Active"
            });

        _userServiceMock
            .Setup(x => x.Authenticate(It.IsAny<List<Claim>>()))
            .ReturnsAsync(new LoginResponse { AccessToken = "jwt-token" });
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
