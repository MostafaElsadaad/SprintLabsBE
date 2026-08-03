using Domain.Enums;
using Domain.Services;

using FluentAssertions;

using Infrastructure.DataAccess;
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

namespace Compass.Tests.Features.InviteOnlyTeacherAuthentication;

public class TeacherIdentityServiceTests
{
    [Theory]
    [InlineData("teacher@example.com")]
    [InlineData("teacher.username")]
    public async Task AuthenticateAsync_accepts_normalized_email_or_username_for_an_eligible_teacher(string identifier)
    {
        await using var context = CreateContext();
        var userManager = CreateUserManager(context);
        var user = new User
        {
            UserName = "teacher.username",
            Email = "teacher@example.com",
            Name = "Teacher",
            IsTeacherAccount = true,
            EmailConfirmed = true,
            Status = UserStatus.Active,
            LockoutEnabled = true
        };
        (await userManager.CreateAsync(user, "StrongPassword123!")).Succeeded.Should().BeTrue();
        var service = new TeacherIdentityService(
            userManager,
            CreateSignInManager(userManager),
            Mock.Of<IRefreshTokenService>(),
            context,
            Options.Create(new TeacherAuthenticationOptions()));

        var result = await service.AuthenticateAsync($" {identifier.ToUpperInvariant()} ", "StrongPassword123!", CancellationToken.None);

        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be("teacher@example.com");
    }

    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static UserManager<User> CreateUserManager(ApplicationDbContext context)
    {
        return new UserManager<User>(
            new UserStore<User, IdentityRole<long>, ApplicationDbContext, long>(context),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            new IUserValidator<User>[] { new UserValidator<User>() },
            new IPasswordValidator<User>[] { new PasswordValidator<User>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<User>>.Instance);
    }

    private static SignInManager<User> CreateSignInManager(UserManager<User> userManager)
    {
        return new SignInManager<User>(
            userManager,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            Options.Create(new IdentityOptions()),
            NullLogger<SignInManager<User>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<User>>());
    }
}
