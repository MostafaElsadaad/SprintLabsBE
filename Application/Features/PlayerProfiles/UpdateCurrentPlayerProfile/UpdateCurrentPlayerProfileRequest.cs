using System.Text.Json;

namespace Application.Features.PlayerProfiles.UpdateCurrentPlayerProfile
{
    public class UpdateCurrentPlayerProfileRequest
    {
        public JsonElement Age { get; set; }
        public JsonElement Grade { get; set; }
        public JsonElement SchoolName { get; set; }
    }
}
