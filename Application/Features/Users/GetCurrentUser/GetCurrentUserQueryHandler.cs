using System.Net;

using Domain.Services;

using MediatR;

using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Users.GetCurrentUser
{
    public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse>
    {
        private readonly IUserService _userService;

        public GetCurrentUserQueryHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<CurrentUserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _userService.GetCurrentUser(request.UserId);
            if (user == null)
            {
                throw new GenericException(
                    message: ErrorMessage.NotFound,
                    statusCode: HttpStatusCode.NotFound,
                    errorCode: ErrorCode.Failure);
            }

            if (user.IsSuspended)
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidAccessToken,
                    statusCode: HttpStatusCode.Forbidden,
                    errorCode: ErrorCode.Failure);
            }

            return new CurrentUserResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                IsPlatformAdmin = user.IsPlatformAdmin,
                PlayerProfileId = user.PlayerProfileId
            };
        }
    }
}