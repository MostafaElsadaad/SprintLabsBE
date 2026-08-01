using Application.Features.Accounts.Common;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using FluentAssertions;

using Moq;

using Shared.Responses;

namespace Compass.Tests.Features.FirebasePlayerAuthentication;

public class FirebaseBusinessBehaviorTests
{
    [Fact]
    public async Task CompleteAsync_UnverifiedEmail_DoesNotActivateStudentOrTeacherState()
    {
        var users = new Mock<IUserService>();
        var players = new Mock<IPlayerRepository>();
        var activation = new Mock<ICommunityLoginActivationService>();
        players.Setup(x => x.GetByUserIdAsync(1)).ReturnsAsync(new Player { Id = 2, UserId = 1, Email = "player@example.com", Name = "Player" });
        users.Setup(x => x.Authenticate(It.IsAny<List<System.Security.Claims.Claim>>())).ReturnsAsync(new LoginResponse());
        var workflow = new ExternalPlayerLoginWorkflow(users.Object, players.Object, activation.Object);

        await workflow.CompleteAsync(new UserIdentityResponse { Id = 1 }, new ExternalPlayerLoginContext
        {
            Subject = "firebase-uid",
            Email = "player@example.com",
            EmailVerified = false,
            Name = "Player",
            ActivatePendingTeacherMemberships = true
        }, CancellationToken.None);

        activation.Verify(x => x.ActivatePendingStudentLicensesAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        activation.Verify(x => x.ActivateEligiblePendingTeacherMembershipsAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
