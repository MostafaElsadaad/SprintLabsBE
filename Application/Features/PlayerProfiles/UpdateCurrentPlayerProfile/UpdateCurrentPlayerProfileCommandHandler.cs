using System.Net;
using System.Text.Json;

using Domain.Repositories;
using Domain.Services;

using MediatR;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Application.Features.PlayerProfiles.UpdateCurrentPlayerProfile
{
    public class UpdateCurrentPlayerProfileCommandHandler : IRequestHandler<UpdateCurrentPlayerProfileCommand, PlayerProfileResponse>
    {
        private readonly IUserService _userService;
        private readonly IPlayerRepository _playerRepository;

        public UpdateCurrentPlayerProfileCommandHandler(
            IUserService userService,
            IPlayerRepository playerRepository)
        {
            _userService = userService;
            _playerRepository = playerRepository;
        }

        public async Task<PlayerProfileResponse> Handle(
            UpdateCurrentPlayerProfileCommand request,
            CancellationToken cancellationToken)
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

            if (TryReadNullableInt(request.Age, out var age))
            {
                if (age is < 1 or > 120)
                {
                    ThrowInvalidInput();
                }

                player.Age = age;
            }

            if (TryReadNullableInt(request.Grade, out var grade))
            {
                if (grade is < 1 or > 20)
                {
                    ThrowInvalidInput();
                }

                player.Grade = grade;
            }

            if (TryReadNullableString(request.SchoolName, out var schoolName))
            {
                if (schoolName?.Length > 200)
                {
                    ThrowInvalidInput();
                }

                player.SchoolName = schoolName;
            }

            var updatedPlayer = await _playerRepository.UpdatePlayer(player);

            return new PlayerProfileResponse
            {
                Id = updatedPlayer.Id,
                Name = updatedPlayer.Name,
                Email = updatedPlayer.Email,
                PictureUrl = updatedPlayer.AvatarUrl,
                Gold = updatedPlayer.Gold,
                Experience = updatedPlayer.Experience,
                Level = updatedPlayer.Level,
                SchoolName = updatedPlayer.SchoolName,
                Age = updatedPlayer.Age,
                Grade = updatedPlayer.Grade
            };
        }

        private static bool TryReadNullableInt(JsonElement value, out int? result)
        {
            result = null;

            if (value.ValueKind == JsonValueKind.Undefined)
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                result = number;
                return true;
            }

            ThrowInvalidInput();
            return false;
        }

        private static bool TryReadNullableString(JsonElement value, out string? result)
        {
            result = null;

            if (value.ValueKind == JsonValueKind.Undefined)
            {
                return false;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                result = value.GetString();
                return true;
            }

            ThrowInvalidInput();
            return false;
        }

        private static void ThrowInvalidInput()
        {
            throw new GenericException(
                message: ErrorMessage.InvalidInput,
                statusCode: HttpStatusCode.BadRequest,
                errorCode: ErrorCode.Failure);
        }
    }
}
