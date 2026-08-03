using Application.Features.Accounts.TeacherAuthentication.TeacherLogin;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.InviteOnlyTeacherAuthentication;

public class TeacherLoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_one_active_community_issues_existing_tokens_and_returns_singular_community()
    {
        await using var context = CreateContext();
        await SeedMembershipAsync(context, CommunityUserStatus.Active);
        var identity = Teacher(20);
        var access = new Mock<IAccessTokenService>();
        access.Setup(x => x.Create(20, "teacher@example.com", "Teacher"))
            .Returns(new AccessTokenResult { AccessToken = "access", AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(5) });
        var refresh = new Mock<IRefreshTokenService>();
        refresh.Setup(x => x.IssueAsync(20, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenResult { RefreshToken = "refresh", RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1) });

        var result = await CreateHandler(context, identity.Object, access.Object, refresh.Object)
            .Handle(new TeacherLoginCommand { Identifier = "teacher@example.com", Password = "password" }, CancellationToken.None);

        result.Community.Id.Should().Be(1);
        result.Community.Name.Should().Be("Community");
        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("refresh");
    }

    [Fact]
    public async Task Handle_pending_or_multiple_current_memberships_fails_before_token_issuance()
    {
        await using var context = CreateContext();
        await SeedMembershipAsync(context, CommunityUserStatus.Pending);
        var identity = Teacher(20);
        var access = new Mock<IAccessTokenService>();
        var refresh = new Mock<IRefreshTokenService>();
        var handler = CreateHandler(context, identity.Object, access.Object, refresh.Object);

        var action = async () => await handler.Handle(new TeacherLoginCommand { Identifier = "teacher", Password = "password" }, CancellationToken.None);

        await action.Should().ThrowAsync<GenericException>();
        access.VerifyNoOtherCalls();
        refresh.VerifyNoOtherCalls();
    }

    private static TeacherLoginCommandHandler CreateHandler(
        ApplicationDbContext context,
        ITeacherIdentityService identity,
        IAccessTokenService access,
        IRefreshTokenService refresh)
    {
        return new TeacherLoginCommandHandler(
            identity,
            access,
            refresh,
            new BaseRepository<CommunityUser>(context),
            NullLogger<TeacherLoginCommandHandler>.Instance);
    }

    private static Mock<ITeacherIdentityService> Teacher(long userId)
    {
        var identity = new Mock<ITeacherIdentityService>();
        identity.Setup(x => x.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherIdentityResult { UserId = userId, Name = "Teacher", Email = "teacher@example.com" });
        return identity;
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static async Task SeedMembershipAsync(ApplicationDbContext context, CommunityUserStatus status)
    {
        context.Communities.Add(new Community { Id = 1, Name = "Community", Slug = "community", Status = CommunityStatus.Active });
        context.CommunityUsers.Add(new CommunityUser { CommunityId = 1, UserId = 20, Role = CommunityUserRole.Teacher, Status = status });
        await context.SaveChangesAsync();
    }
}
