namespace Domain.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string name, string resetUrl, CancellationToken cancellationToken);
    Task SendCommunityInvitationEmailAsync(string email, string name, string communityName, string invitationUrl, CancellationToken cancellationToken);
}