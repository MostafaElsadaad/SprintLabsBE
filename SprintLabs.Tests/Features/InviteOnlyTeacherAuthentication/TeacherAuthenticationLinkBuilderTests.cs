using Application.Features.Accounts.TeacherAuthentication.Common;

using FluentAssertions;

using Shared.Options;

namespace Compass.Tests.Features.InviteOnlyTeacherAuthentication;

public class TeacherAuthenticationLinkBuilderTests
{
    private readonly FrontendOptions _frontend = new() { BaseUrl = "https://app.example/" };

    [Fact]
    public void Invitation_uses_the_teacher_setup_route_and_url_encodes_the_raw_token()
    {
        var url = TeacherAuthenticationLinkBuilder.Invitation(_frontend, "raw+/token");

        url.Should().StartWith("https://app.example/invitations/teacher/setup?token=");
        url.Should().Contain("raw%2B%2Ftoken");
    }

    [Fact]
    public void PasswordReset_uses_user_id_and_url_safe_identity_token()
    {
        var url = TeacherAuthenticationLinkBuilder.PasswordReset(_frontend, 42, "identity token");

        url.Should().StartWith("https://app.example/reset-password?userId=42&token=");
        url.Should().NotContain("email=");
    }
}
