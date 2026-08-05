using Application.Features.Accounts.CommunityAuthentication.CommunityLogin;

using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using Shared.Options;
using Shared.Responses;
using Shared.Enums;

namespace Compass.Tests.Features.InviteOnlyTeacherAuthentication;

public class InviteOnlyTeacherAuthenticationLifecycleTests
{
    [Fact]
    public async Task Invited_teacher_can_complete_login_and_reset_without_duplicate_artifacts()
    {
        await using var context = CreateContext();
        await SeedOwnerAndCommunityAsync(context);
        var userManager = CreateUserManager(context);
        var refresh = new Mock<IRefreshTokenService>();
        var invitationService = new TeacherInvitationService(
            context,
            userManager,
            refresh.Object,
            Options.Create(new TeacherAuthenticationOptions { InvitationLifetimeDays = 7 }));

        var invitation = await invitationService.IssueAsync(10, 1, "teacher@example.com", CancellationToken.None);
        var validation = await invitationService.ValidateAsync(invitation.InvitationToken!, CancellationToken.None);

        validation.CommunityName.Should().Be("Community");
        validation.MaskedEmail.Should().Be("t***r@example.com");

        await invitationService.CompleteAsync(invitation.InvitationToken!, "Teacher", "StrongPassword123!", CancellationToken.None);

        var identityService = new TeacherIdentityService(
            userManager,
            CreateSignInManager(userManager),
            refresh.Object,
            context,
            Options.Create(new TeacherAuthenticationOptions()));
        var access = new Mock<IAccessTokenService>();
        access.Setup(x => x.Create(It.IsAny<long>(), "teacher@example.com", "Teacher", AuthenticatedAccountType.Teacher))
            .Returns(new AccessTokenResult { AccessToken = "access", AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(5) });
        refresh.Setup(x => x.IssueAsync(It.IsAny<long>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenResult { RefreshToken = "refresh", RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1) });
        var loginHandler = new CommunityLoginCommandHandler(
            identityService,
            access.Object,
            refresh.Object,
            new BaseRepository<CommunityUser>(context),
            NullLogger<CommunityLoginCommandHandler>.Instance);

        var login = await loginHandler.Handle(new CommunityLoginCommand
        {
            Identifier = "teacher@example.com",
            Password = "StrongPassword123!"
        }, CancellationToken.None);

        login.Community.Id.Should().Be(1);
        login.AccessToken.Should().Be("access");

        var reset = await identityService.CreatePasswordResetAsync("teacher@example.com", CancellationToken.None);
        reset.ResetToken.Should().NotBeNullOrWhiteSpace();
        await identityService.ResetPasswordAsync(reset.UserId, reset.ResetToken!, "NewStrongPassword123!", CancellationToken.None);

        refresh.Verify(x => x.RevokeAllForUserAsync(reset.UserId, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        (await context.CommunityUsers.Where(x => x.UserId == reset.UserId).ToListAsync())
            .Should().ContainSingle(x => x.Role == CommunityUserRole.Teacher && x.Status == CommunityUserStatus.Active);
        (await context.Players.CountAsync()).Should().Be(0);
        (await context.StudentLicenses.CountAsync()).Should().Be(0);
        (await context.CommunityLicenses.SingleAsync()).UsedTeachers.Should().Be(1);
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static async Task SeedOwnerAndCommunityAsync(ApplicationDbContext context)
    {
        context.Users.Add(new User
        {
            Id = 10,
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            Name = "Owner",
            EmailConfirmed = true
        });
        context.Communities.Add(new Community { Id = 1, Name = "Community", Slug = "community", Status = CommunityStatus.Active });
        context.CommunityUsers.Add(new CommunityUser { CommunityId = 1, UserId = 10, Role = CommunityUserRole.Owner, Status = CommunityUserStatus.Active });
        context.CommunityLicenses.Add(new CommunityLicense { CommunityId = 1, MaxTeachers = 5, UsedTeachers = 0 });
        await context.SaveChangesAsync();
    }

    private static UserManager<User> CreateUserManager(ApplicationDbContext context)
    {
        var manager = new UserManager<User>(
            new UserStore<User, IdentityRole<long>, ApplicationDbContext, long>(context),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            new IUserValidator<User>[] { new UserValidator<User>() },
            new IPasswordValidator<User>[] { new PasswordValidator<User>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<User>>.Instance);
        manager.RegisterTokenProvider(TokenOptions.DefaultProvider, new EmailTokenProvider<User>());
        return manager;
    }

    private static SignInManager<User> CreateSignInManager(UserManager<User> userManager) => new(
        userManager,
        new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
        Mock.Of<IUserClaimsPrincipalFactory<User>>(),
        Options.Create(new IdentityOptions()),
        NullLogger<SignInManager<User>>.Instance,
        Mock.Of<IAuthenticationSchemeProvider>(),
        Mock.Of<IUserConfirmation<User>>());
}
