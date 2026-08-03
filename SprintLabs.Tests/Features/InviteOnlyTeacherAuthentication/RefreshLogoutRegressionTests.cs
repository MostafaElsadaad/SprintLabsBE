using Application.Features.Accounts.TeacherAuthentication.Logout;
using Application.Features.Accounts.TeacherAuthentication.RefreshToken;

using Domain.Services;

using FluentAssertions;

using Moq;

using Shared.Responses;

namespace Compass.Tests.Features.InviteOnlyTeacherAuthentication;

public class RefreshLogoutRegressionTests
{
    [Fact]
    public async Task Refresh_uses_existing_rotation_identity_and_access_token_services()
    {
        var refresh = new Mock<IRefreshTokenService>();
        refresh.Setup(x => x.RotateAsync("raw", "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenResult { UserId = 42, RefreshToken = "replacement", RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1) });
        var identity = new Mock<ITeacherIdentityService>();
        identity.Setup(x => x.GetTeacherAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TeacherIdentityResult { UserId = 42, Email = "teacher@example.com", Name = "Teacher" });
        var access = new Mock<IAccessTokenService>();
        access.Setup(x => x.Create(42, "teacher@example.com", "Teacher"))
            .Returns(new AccessTokenResult { AccessToken = "access", AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15) });

        var result = await new RefreshTokenCommandHandler(refresh.Object, identity.Object, access.Object)
            .Handle(new RefreshTokenCommand { RefreshToken = "raw", RevokedByIp = "127.0.0.1" }, CancellationToken.None);

        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("replacement");
    }

    [Fact]
    public async Task Logout_delegates_to_existing_refresh_token_revocation_service()
    {
        var refresh = new Mock<IRefreshTokenService>();

        await new LogoutCommandHandler(refresh.Object)
            .Handle(new LogoutCommand { RefreshToken = "raw", RevokedByIp = "127.0.0.1" }, CancellationToken.None);

        refresh.Verify(x => x.RevokeAsync("raw", "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
