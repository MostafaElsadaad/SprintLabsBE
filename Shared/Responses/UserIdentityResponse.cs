namespace Shared.Responses
{
    public class UserIdentityResponse
    {
        public long Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsSuspended { get; set; }
    }
}