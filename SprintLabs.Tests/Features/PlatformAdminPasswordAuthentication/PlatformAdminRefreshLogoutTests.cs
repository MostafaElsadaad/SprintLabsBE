using Application.Features.Accounts.TeacherAuthentication.RefreshToken;

using Domain.Services;

using FluentAssertions;

using Moq;

using Shared.Enums;
using Shared.Responses;

namespace Compass.Tests.Features.PlatformAdminPasswordAuthentication;

public class PlatformAdminRefreshLogoutTests
{
    [Fact]
    public async Task Refresh_uses_the_shared_rotation_service_and_mints_a_platform_admin_access_token()
    {
        var refresh = new Mock<IRefreshTokenService>();
        refresh.Setup(x => x.RotateAsync("raw", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenResult
            {
                UserId = 9,
                Email = "admin@example.com",
                Name = "Admin",
                AccountType = AuthenticatedAccountType.PlatformAdmin,
                RefreshToken = "replacement",
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1)
            });
        var access = new Mock<IAccessTokenService>();
        access.Setup(x => x.Create(9, "admin@example.com", "Admin", AuthenticatedAccountType.PlatformAdmin))
            .Returns(new AccessTokenResult { AccessToken = "access", AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15) });

        var result = await new RefreshTokenCommandHandler(refresh.Object, access.Object)
            .Handle(new RefreshTokenCommand { RefreshToken = "raw" }, CancellationToken.None);

        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("replacement");
    }
}
