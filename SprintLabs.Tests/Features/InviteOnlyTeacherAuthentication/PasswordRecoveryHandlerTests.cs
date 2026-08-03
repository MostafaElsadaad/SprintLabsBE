using Application.Features.Accounts.TeacherAuthentication.ForgotPassword;
using Application.Features.Accounts.TeacherAuthentication.ResetPassword;

using Domain.Services;

using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Options;
using Shared.Responses;

namespace Compass.Tests.Features.InviteOnlyTeacherAuthentication;

public class PasswordRecoveryHandlerTests
{
    [Fact]
    public async Task ForgotPassword_uses_identifier_and_user_id_link_for_eligible_teacher()
    {
        var identity = new Mock<ITeacherIdentityService>();
        identity.Setup(x => x.CreatePasswordResetAsync("teacher", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordResetDispatchResult { UserId = 42, Email = "teacher@example.com", Name = "Teacher", ResetToken = "token" });
        var email = new Mock<IEmailService>();
        var handler = new ForgotPasswordCommandHandler(identity.Object, email.Object, Options.Create(new FrontendOptions { BaseUrl = "https://app.example" }));

        await handler.Handle(new ForgotPasswordCommand { Identifier = "teacher" }, CancellationToken.None);

        email.Verify(x => x.SendPasswordResetEmailAsync("teacher@example.com", "Teacher", It.Is<string>(url => url.Contains("userId=42") && !url.Contains("email=")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_swallows_delivery_failure_to_preserve_generic_public_response()
    {
        var identity = new Mock<ITeacherIdentityService>();
        identity.Setup(x => x.CreatePasswordResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordResetDispatchResult { UserId = 42, Email = "teacher@example.com", Name = "Teacher", ResetToken = "token" });
        var email = new Mock<IEmailService>();
        email.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());
        var handler = new ForgotPasswordCommandHandler(identity.Object, email.Object, Options.Create(new FrontendOptions { BaseUrl = "https://app.example" }));

        var action = async () => await handler.Handle(new ForgotPasswordCommand { Identifier = "unknown" }, CancellationToken.None);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ResetPassword_invalid_transport_token_returns_safe_error()
    {
        var handler = new ResetPasswordCommandHandler(Mock.Of<ITeacherIdentityService>());

        var action = async () => await handler.Handle(new ResetPasswordCommand { UserId = 42, Token = "%", NewPassword = "StrongPassword123!" }, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<GenericException>();
        exception.Which.ErrorCode.Should().Be(ErrorCode.InvalidOrExpiredPasswordResetToken);
    }
}
