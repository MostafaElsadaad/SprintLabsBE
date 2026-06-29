using System.Net;

using Domain.Enums;
using Domain.Services;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Communities.StudentLicenses.Common;

public static class StudentLicenseAuthorization
{
    public static async Task EnsureActiveOwner(
        IUserService userService,
        ICommunityAccessService communityAccessService,
        long userId,
        long communityId)
    {
        var user = await userService.GetCurrentUser(userId);
        if (user == null)
        {
            throw NotFound();
        }

        if (user.IsSuspended)
        {
            throw Forbidden();
        }

        var isOwner = await communityAccessService.HasCommunityRole(
            userId,
            communityId,
            new[] { CommunityUserRole.Owner });
        if (!isOwner)
        {
            throw Forbidden();
        }
    }

    private static GenericException Forbidden()
    {
        return new GenericException(
            message: ErrorMessage.InvalidAccessToken,
            statusCode: HttpStatusCode.Forbidden,
            errorCode: ErrorCode.Failure);
    }

    private static GenericException NotFound()
    {
        return new GenericException(
            message: ErrorMessage.NotFound,
            statusCode: HttpStatusCode.NotFound,
            errorCode: ErrorCode.Failure);
    }
}
