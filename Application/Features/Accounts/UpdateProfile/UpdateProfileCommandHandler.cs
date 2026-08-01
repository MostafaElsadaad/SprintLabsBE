using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Shared.Exceptions;

using Shared.Enums;
using Shared.Responses;

namespace Application.Features.Accounts.UpdateProfile
{
    public class UpdateProfileCommandHandler: IRequestHandler<UpdateProfileCommand, BaseResponse<PlayerProfileResponse>>
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IUserService _userService;

        public UpdateProfileCommandHandler(IPlayerRepository playerRepository, IUserService userService)
        {
            _playerRepository = playerRepository;
            _userService = userService;
        }

        public async Task<BaseResponse<PlayerProfileResponse>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate first
            if (request.Name != null && string.IsNullOrWhiteSpace(request.Name))
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidInput,
                    statusCode: HttpStatusCode.BadRequest,
                    errorCode: ErrorCode.Failure);
            }

            if (request.Grade != null && (request.Grade < 1 || request.Grade > 20))
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidInput,
                    statusCode: HttpStatusCode.BadRequest,
                    errorCode: ErrorCode.Failure);
            }



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

            // If no player was found

            if ( player == null)
            {

                throw new GenericException(
                    message: ErrorMessage.NotFound,
                    statusCode: HttpStatusCode.NotFound,
                    errorCode: ErrorCode.Failure);
            }
            else
            {
                if (request.Name != null)
                {
                     player.Name = request.Name;
                }
                if (request.Age != null)
                {
                     player.Age = request.Age;
                }

                if (request.SchoolName != null)
                {
                    player.SchoolName = request.SchoolName;
                }

                if (request.Grade != null)
                {
                    player.Grade = request.Grade;
                }
            }

            // Update fields
            var updatedPlayer = await _playerRepository.UpdatePlayer(player);

            var response = new PlayerProfileResponse
            {
                Id = updatedPlayer.Id,
                Name = updatedPlayer.Name,
                Age= updatedPlayer.Age,
                Email=updatedPlayer.Email,
                Grade=updatedPlayer.Grade,
                SchoolName = updatedPlayer.SchoolName,
                Level=updatedPlayer.Level,
                Gold=updatedPlayer.Gold,
                Experience=updatedPlayer.Experience,
                PictureUrl = updatedPlayer.AvatarUrl,
                
                
            };
            return new BaseResponse<PlayerProfileResponse>(
                response,"Updated with success",HttpStatusCode.OK,ErrorCode.Success);
                

        }

    }
}
