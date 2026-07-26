using Shared.Helpers;
using Shared.Options;

namespace Application.Features.Accounts.TeacherAuthentication.Common;

public static class TeacherAuthenticationLinkBuilder
{
    public static string Confirmation(FrontendOptions options, long userId, string token)
    {
        return $"{BaseUrl(options)}/confirm-email?userId={userId}&token={Uri.EscapeDataString(UrlSafeTokenHelper.Encode(token))}";
    }

    public static string PasswordReset(FrontendOptions options, string email, string token)
    {
        return $"{BaseUrl(options)}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(UrlSafeTokenHelper.Encode(token))}";
    }

    public static string Invitation(FrontendOptions options, string token)
    {
        return $"{BaseUrl(options)}/invitations/accept?token={Uri.EscapeDataString(token)}";
    }

    private static string BaseUrl(FrontendOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl)) throw new InvalidOperationException("Frontend base URL is not configured.");
        return options.BaseUrl.TrimEnd('/');
    }
}
