using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Domain.Models;
using Domain.Repositories;

using MediatR;

using Microsoft.AspNetCore.Http;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;
namespace Application.Features.Accounts.GetPlayer
{
    public class GetPlayerQueryHandler : IRequestHandler<GetPlayerQuery, BaseResponse<PlayerProfileResponse>>
    {
        private readonly IBaseRepository<Player> _playerRepository;

        public GetPlayerQueryHandler(IBaseRepository<Player> playerRepository)
        {
            _playerRepository = playerRepository;
        }

        public async Task<BaseResponse<PlayerProfileResponse>> Handle(GetPlayerQuery request, CancellationToken cancellationToken)
        {
            // 1. Find player by GoogleId
            var player = await _playerRepository.GetByCustomConditionAsync(p => p.GoogleId == request.GoogleId);


            // 2. If not found -> throw error

            if (player == null)
            {
                throw new GenericException(
                   message: ErrorMessage.NotFound,
                   statusCode: HttpStatusCode.NotFound,
                   errorCode: ErrorCode.Failure);

            }

            // 3. Map to PlayerProfileResponse

         

            var response = new PlayerProfileResponse
            {
                Id = player.Id,
                Name = player.Name,
                Age = player.Age,
                Email = player.Email,
                Grade = player.Grade,
                SchoolName = player.SchoolName,
                Level = player.Level,
                Gold = player.Gold,
                Experience = player.Experience,
                PictureUrl = player.AvatarUrl,


            };
            return new BaseResponse<PlayerProfileResponse>(
                response, "retrieved with success", HttpStatusCode.OK, ErrorCode.Success);


            // 4. Return BaseResponse
        }
    }
}