using System.Net;

using API.Controllers;

using Application.Features.Accounts.FirebaseAuthenticate;

using FluentAssertions;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using Moq;

using Shared.Responses;

namespace Compass.Tests.Features.FirebasePlayerAuthentication;

public class FirebaseAuthenticationControllerTests
{
    [Fact]
    public async Task FirebaseLogin_ValidJsonBody_ReturnsExistingBaseResponseWrapper()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<FirebaseAuthenticationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse { AccessToken = "jwt", UserId = 1, PlayerProfileId = 2, Name = "Player", Email = "player@example.com", Gold = 0, Experience = 0, Level = 1 });
        var controller = new AccountController(mediator.Object);

        var action = await controller.FirebaseLogin(new FirebaseAuthenticationRequest { IdToken = "firebase-token" });

        var ok = action.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<BaseResponse<LoginResponse>>().Subject;
        response.Data.AccessToken.Should().Be("jwt");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        mediator.Verify(x => x.Send(It.Is<FirebaseAuthenticationCommand>(c => c.IdToken == "firebase-token"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
