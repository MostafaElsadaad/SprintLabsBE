namespace Application.Features.Users.GetCurrentUser
{
    public class CurrentUserResponse
    {
        public long UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsPlatformAdmin { get; set; }
        public long? PlayerProfileId { get; set; }
    }
}