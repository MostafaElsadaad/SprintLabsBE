using System.Net;

using Domain.Repositories;
using Domain.Services;

using MediatR;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Features.Users.GetCurrentPlayerProfile
{
    public class GetCurrentPlayerProfileQueryHandler : IRequestHandler<GetCurrentPlayerProfileQuery, PlayerProfileResponse>
    {
        private readonly IUserService _userService;
        private readonly IPlayerRepository _playerRepository;

        public GetCurrentPlayerProfileQueryHandler(
            IUserService userService,
            IPlayerRepository playerRepository)
        {
            _userService = userService;
            _playerRepository = playerRepository;
        }

        public async Task<PlayerProfileResponse> Handle(GetCurrentPlayerProfileQuery request, CancellationToken cancellationToken)
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

            var player = await _playerRepository.GetByUserIdAsync(request.UserId);
            if (player == null)
            {
                throw new GenericException(
                    message: ErrorMessage.NotFound,
                    statusCode: HttpStatusCode.NotFound,
                    errorCode: ErrorCode.Failure);
            }

            return new PlayerProfileResponse
            {
                Id = player.Id,
                Name = player.Name,
                Email = player.Email,
                PictureUrl = player.AvatarUrl,
                Gold = player.Gold,
                Experience = player.Experience,
                Level = player.Level,
                SchoolName = player.SchoolName,
                Age = player.Age,
                Grade = player.Grade
            };
        }
    }
}