using System.Net;

using Application.Features.Accounts.Common;
using Application.Features.Accounts.FirebaseAuthenticate;

using Domain.Services;

using FluentAssertions;

using Moq;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.FirebasePlayerAuthentication;

public class FirebaseAuthenticationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidVerifiedIdentity_ResolvesUserAndDelegatesToWorkflow()
    {
        var firebase = new Mock<IFirebaseAuthenticationService>();
        var users = new Mock<IUserService>();
        var workflow = new Mock<IExternalPlayerLoginWorkflow>();
        firebase.Setup(x => x.VerifyIdTokenAsync("firebase-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FirebaseUserResponse { Uid = "firebase-uid", Email = "player@example.com", EmailVerified = true, Name = "Player" });
        users.Setup(x => x.FindOrCreateFirebaseUser(It.IsAny<FirebaseUserResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserIdentityResponse { Id = 7, Email = "player@example.com", Name = "Player" });
        workflow.Setup(x => x.CompleteAsync(It.IsAny<UserIdentityResponse>(), It.IsAny<ExternalPlayerLoginContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse { AccessToken = "sprintlabs-token", UserId = 7, PlayerProfileId = 9 });

        var result = await new FirebaseAuthenticationCommandHandler(firebase.Object, users.Object, workflow.Object)
            .Handle(new FirebaseAuthenticationCommand { IdToken = "firebase-token" }, CancellationToken.None);

        result.AccessToken.Should().Be("sprintlabs-token");
        workflow.Verify(x => x.CompleteAsync(
            It.IsAny<UserIdentityResponse>(),
            It.Is<ExternalPlayerLoginContext>(c => c.Subject == "firebase-uid" && c.EmailVerified && c.ActivatePendingTeacherMemberships),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_BlankToken_ReturnsSafeUnauthorized(string idToken)
    {
        var firebase = new Mock<IFirebaseAuthenticationService>();
        var handler = new FirebaseAuthenticationCommandHandler(firebase.Object, new Mock<IUserService>().Object, new Mock<IExternalPlayerLoginWorkflow>().Object);

        var action = () => handler.Handle(new FirebaseAuthenticationCommand { IdToken = idToken }, CancellationToken.None);

        var error = await action.Should().ThrowAsync<GenericException>();
        error.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        error.Which.Message.Should().Be(ErrorMessage.InvalidAccessToken);
        firebase.Verify(x => x.VerifyIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidProviderToken_PropagatesSafeUnauthorizedWithoutTokenDisclosure()
    {
        var firebase = new Mock<IFirebaseAuthenticationService>();
        firebase.Setup(x => x.VerifyIdTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GenericException(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Unauthorized));
        var handler = new FirebaseAuthenticationCommandHandler(firebase.Object, new Mock<IUserService>().Object, new Mock<IExternalPlayerLoginWorkflow>().Object);

        var action = () => handler.Handle(new FirebaseAuthenticationCommand { IdToken = "not-disclosed" }, CancellationToken.None);

        var error = await action.Should().ThrowAsync<GenericException>();
        error.Which.Message.Should().Be(ErrorMessage.InvalidAccessToken);
        error.Which.Message.Should().NotContain("not-disclosed");
    }
}
