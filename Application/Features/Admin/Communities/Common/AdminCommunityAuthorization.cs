using System.Net;

using Domain.Services;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Admin.Communities.Common;

internal static class AdminCommunityAuthorization
{
    public static async Task EnsurePlatformAdmin(IUserService userService, long authenticatedUserId)
    {
        var user = await userService.GetCurrentUser(authenticatedUserId);
        if (user == null || user.IsSuspended || !user.IsPlatformAdmin)
        {
            throw new GenericException(
                message: ErrorMessage.InvalidAccessToken,
                statusCode: HttpStatusCode.Forbidden,
                errorCode: ErrorCode.Failure);
        }
    }
}
