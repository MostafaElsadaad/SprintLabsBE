using Shared.Helpers;
using Shared.Options;

namespace Application.Features.Accounts.TeacherAuthentication.Common;

public static class TeacherAuthenticationLinkBuilder
{
    public static string PasswordReset(FrontendOptions options, long userId, string token)
    {
        return $"{BaseUrl(options)}/reset-password?userId={userId}&token={Uri.EscapeDataString(UrlSafeTokenHelper.Encode(token))}";
    }

    public static string Invitation(FrontendOptions options, string token)
    {
        return $"{BaseUrl(options)}/invitations/teacher/setup?token={Uri.EscapeDataString(token)}";
    }

    private static string BaseUrl(FrontendOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl)) throw new InvalidOperationException("Frontend base URL is not configured.");
        return options.BaseUrl.TrimEnd('/');
    }
}