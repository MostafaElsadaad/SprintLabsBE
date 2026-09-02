using System.Net;

using API.Controllers;

using Application.Features.Accounts.DevelopmentAuthentication.DevelopmentLogin;
using Application.Features.Accounts.DevelopmentAuthentication.ListDevelopmentPlayers;

using FluentAssertions;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using Shared.Options;
using Shared.Responses;

namespace Compass.Tests.Features.DevelopmentPlayerAuthentication;

public class DevelopmentAuthenticationControllerTests
{
    [Fact]
    public async Task DevelopmentLogin_ReturnsExistingEnvelopeAndForwardsAllHeaderValues()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<DevelopmentLoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse { AccessToken = "jwt", UserId = 1, PlayerProfileId = 2 });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append(DevelopmentAuthenticationOptions.HeaderName, "first");
        httpContext.Request.Headers.Append(DevelopmentAuthenticationOptions.HeaderName, "second");
        var controller = new AccountController(mediator.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var action = await controller.DevelopmentLogin(new DevelopmentLoginRequest { AccountKey = "dev-player-01" });

        var ok = action.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<BaseResponse<LoginResponse>>().Subject;
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.AccessToken.Should().Be("jwt");
        mediator.Verify(x => x.Send(
            It.Is<DevelopmentLoginCommand>(command => command.AccountKey == "dev-player-01" && command.SuppliedApiKeys.SequenceEqual(new[] { "first", "second" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDevelopmentPlayers_ReturnsSafeEnvelopeAndForwardsHeader()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<ListDevelopmentPlayersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DevelopmentPlayerResponse>
            {
                new() { AccountKey = "dev-player-01", DisplayName = "Dev Player 01" }
            });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append(DevelopmentAuthenticationOptions.HeaderName, "development-key");
        var controller = new AccountController(mediator.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var action = await controller.GetDevelopmentPlayers();

        var ok = action.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<BaseResponse<List<DevelopmentPlayerResponse>>>().Subject;
        response.Data.Single().AccountKey.Should().Be("dev-player-01");
        mediator.Verify(x => x.Send(
            It.Is<ListDevelopmentPlayersQuery>(query => query.SuppliedApiKeys.SequenceEqual(new[] { "development-key" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
