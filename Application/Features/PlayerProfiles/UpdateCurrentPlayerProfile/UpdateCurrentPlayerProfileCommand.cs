using System.Text.Json;

using MediatR;

using Shared.Responses;

namespace Application.Features.PlayerProfiles.UpdateCurrentPlayerProfile
{
    public class UpdateCurrentPlayerProfileCommand : IRequest<PlayerProfileResponse>
    {
        public long UserId { get; set; }
        public JsonElement Age { get; set; }
        public JsonElement Grade { get; set; }
        public JsonElement SchoolName { get; set; }
    }
}
