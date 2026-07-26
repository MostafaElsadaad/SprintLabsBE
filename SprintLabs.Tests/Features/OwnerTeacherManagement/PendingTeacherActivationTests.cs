using System.Security.Claims;

using Application.Features.Accounts.GoogleAuthenticate;

using Domain.Enums;
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

namespace Compass.Tests.Features.OwnerTeacherManagement;

public class PendingTeacherActivationTests
{
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IGoogleAuthenticationService> _googleAuthenticationServiceMock = new();
    private readonly Mock<IPlayerRepository> _playerRepositoryMock = new();

    [Fact]
    public async Task Handle_MatchingPendingTeacherMemberships_RemainsPendingUntilExplicitAcceptance()
    {
        await using var context = CreateContext();
        await SeedPendingTeacherMemberships(context);
        SetupGoogleLogin();
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GoogleAuthenticationCommand { IdToken = "google-token" },
            CancellationToken.None);

        result.AccessToken.Should().Be("jwt-token");
        result.UserId.Should().Be(20);
        result.PlayerProfileId.Should().Be(30);
        result.Email.Should().Be("teacher@example.com");
        result.Gold.Should().Be(5);
        result.Experience.Should().Be(50);
        result.Level.Should().Be(2);

        var activated = await context.CommunityUsers
            .Where(x => x.UserId == 20 && x.Role == CommunityUserRole.Teacher)
            .ToListAsync();
        activated.Should().HaveCount(2);
        activated.Should().OnlyContain(x => x.Status == CommunityUserStatus.Pending);
        activated.Should().OnlyContain(x => x.UpdatedAt == null);

        var otherUserPending = await context.CommunityUsers.SingleAsync(x => x.UserId == 21);
        otherUserPending.Status.Should().Be(CommunityUserStatus.Pending);

        var license = await context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1);
        license.UsedTeachers.Should().Be(2);
    }

    private GoogleAuthenticationCommandHandler CreateHandler(ApplicationDbContext context)
    {
        var activationService = new CommunityLoginActivationService(
            new BaseRepository<CommunityUser>(context),
            new BaseRepository<StudentLicense>(context));

        return new GoogleAuthenticationCommandHandler(
            _userServiceMock.Object,
            _googleAuthenticationServiceMock.Object,
            _playerRepositoryMock.Object,
            activationService);
    }

    private void SetupGoogleLogin()
    {
        _googleAuthenticationServiceMock
            .Setup(x => x.GetUserInfo("google-token"))
            .ReturnsAsync(new GoogleUserResponse
            {
                Sub = "google-sub",
                Email = "teacher@example.com",
                Name = "Teacher Name",
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
                Email = "teacher@example.com",
                Name = "Teacher Name",
                Status = "Active"
            });

        _userServiceMock
            .Setup(x => x.Authenticate(It.IsAny<List<Claim>>()))
            .ReturnsAsync(new LoginResponse { AccessToken = "jwt-token" });

        _playerRepositoryMock
            .Setup(x => x.GetByUserIdAsync(20))
            .ReturnsAsync(new Player
            {
                Id = 30,
                UserId = 20,
                GoogleId = "google-sub",
                Email = "teacher@example.com",
                Name = "Teacher Name",
                Gold = 5,
                Experience = 50,
                Level = 2
            });
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task SeedPendingTeacherMemberships(ApplicationDbContext context)
    {
        context.Communities.AddRange(
            new Community { Id = 1, Name = "One", Slug = "one" },
            new Community { Id = 2, Name = "Two", Slug = "two" },
            new Community { Id = 3, Name = "Three", Slug = "three" });

        context.CommunityUsers.AddRange(
            new CommunityUser
            {
                CommunityId = 1,
                UserId = 20,
                Role = CommunityUserRole.Teacher,
                Status = CommunityUserStatus.Pending
            },
            new CommunityUser
            {
                CommunityId = 2,
                UserId = 20,
                Role = CommunityUserRole.Teacher,
                Status = CommunityUserStatus.Pending
            },
            new CommunityUser
            {
                CommunityId = 3,
                UserId = 21,
                Role = CommunityUserRole.Teacher,
                Status = CommunityUserStatus.Pending
            });

        context.CommunityLicenses.Add(new CommunityLicense
        {
            CommunityId = 1,
            MaxTeachers = 5,
            UsedTeachers = 2
        });

        await context.SaveChangesAsync();
    }
}
