namespace Domain.Services;

public interface IEmailService
{
    Task SendConfirmationEmailAsync(string email, string name, string confirmationUrl, CancellationToken cancellationToken);
    Task SendPasswordResetEmailAsync(string email, string name, string resetUrl, CancellationToken cancellationToken);
    Task SendCommunityInvitationEmailAsync(string email, string name, string communityName, string invitationUrl, CancellationToken cancellationToken);
}
