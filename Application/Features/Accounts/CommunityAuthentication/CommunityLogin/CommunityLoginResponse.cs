using Shared.Responses;
using System.Text.Json.Serialization;

namespace Application.Features.Accounts.CommunityAuthentication.CommunityLogin;

public class CommunityLoginResponse : TeacherTokenResponse
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TeacherCommunityResponse? Community { get; set; }
}
