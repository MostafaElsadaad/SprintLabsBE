using System.Security.Claims;

using API.Controllers;

using Application.Features.Accounts.UpdateProfile;

using FluentAssertions;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using Shared.Enums;
using Shared.Responses;

namespace Compass.Tests.Features.AccountProfile;

public class AccountControllerProfileTests
{
    [Fact]
    public async Task UpdateProfile_UsesInternalUserIdClaimAndIgnoresSubject()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<UpdateProfileCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BaseResponse<PlayerProfileResponse>(new PlayerProfileResponse(), "ok", System.Net.HttpStatusCode.OK, ErrorCode.Success));
        var controller = new AccountController(mediator.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "external-subject"), new Claim("userId", "7") }, "test")) } }
        };

        var action = await controller.UpdateProfile(new UpdateProfileCommand { Name = "Player" });

        action.Should().BeOfType<OkObjectResult>();
        mediator.Verify(x => x.Send(It.Is<UpdateProfileCommand>(c => c.UserId == 7), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_SubjectOnlyToken_IsUnauthorized()
    {
        var controller = new AccountController(new Mock<IMediator>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "firebase-uid") }, "test")) } }
        };

        var action = await controller.UpdateProfile(new UpdateProfileCommand());

        action.Should().BeOfType<UnauthorizedResult>();
    }
}
