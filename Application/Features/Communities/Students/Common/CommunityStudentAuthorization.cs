using System.Net;

using Domain.Enums;
using Domain.Services;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Communities.Students.Common;

public static class CommunityStudentAuthorization
{
    private static readonly CommunityUserRole[] AllowedRoles =
    {
        CommunityUserRole.Owner,
        CommunityUserRole.Teacher
    };

    public static async Task EnsureCanViewStudents(
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

        var canView = await communityAccessService.HasCommunityRole(
            userId,
            communityId,
            AllowedRoles);
        if (!canView)
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
