using Application.Features.Accounts.CommunityAuthentication.RegisterCommunity;

using Domain.Services;

using Moq;

namespace Compass.Tests.Features.CommunityAuthentication;

public class RegisterCommunityCommandHandlerTests
{
    [Fact]
    public async Task Handle_delegates_only_token_name_and_password_to_the_shared_invitation_completion_service()
    {
        var invitations = new Mock<ITeacherInvitationService>();
        var handler = new RegisterCommunityCommandHandler(invitations.Object);

        await handler.Handle(new RegisterCommunityCommand
        {
            Token = "setup-token",
            Name = "Community Admin",
            Password = "StrongPassword123!"
        }, CancellationToken.None);

        invitations.Verify(x => x.CompleteAsync(
            "setup-token",
            "Community Admin",
            "StrongPassword123!",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
