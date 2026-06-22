using MediatR;

using Shared.Responses;

namespace Application.Features.Users.GetCurrentPlayerProfile
{
    public class GetCurrentPlayerProfileQuery : IRequest<PlayerProfileResponse>
    {
        public long UserId { get; set; }
    }
}