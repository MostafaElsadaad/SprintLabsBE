using System.Net;
using System.Net.Mail;

using Domain.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Options;

namespace Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string name, string resetUrl, CancellationToken cancellationToken)
    {
        var body = $"<p>Hello {WebUtility.HtmlEncode(name)},</p><p>Use this link to reset your SprintLabs password.</p><p><a href=\"{WebUtility.HtmlEncode(resetUrl)}\">Reset password</a></p>";
        return SendAsync(email, "Reset your SprintLabs password", body, cancellationToken);
    }

    public Task SendCommunityInvitationEmailAsync(string email, string name, string communityName, string invitationUrl, CancellationToken cancellationToken)
    {
        var body = $"<p>Hello,</p><p>You were invited to join {WebUtility.HtmlEncode(communityName)} as a teacher.</p><p><a href=\"{WebUtility.HtmlEncode(invitationUrl)}\">Set up your teacher account</a></p>";
        return SendAsync(email, $"Invitation to {communityName}", body, cancellationToken);
    }

    private async Task SendAsync(string email, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.SenderEmail))
        {
            throw new InvalidOperationException("SMTP email is not configured.");
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Timeout = _options.TimeoutMilliseconds,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(_options.Username)
        };
        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        using var message = new MailMessage(new MailAddress(_options.SenderEmail, _options.SenderName), new MailAddress(email))
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (SmtpException exception)
        {
            _logger.LogWarning(exception, "SMTP delivery failed for recipient domain {RecipientDomain}.", GetDomain(email));
            throw new InvalidOperationException("Unable to send email.", exception);
        }
    }

    private static string GetDomain(string email)
    {
        return email.Split('@').LastOrDefault() ?? "unknown";
    }
}