namespace Shared.Responses
{
    public class UserIdentityResponse
    {
        public long Id { get; set; }
        public string? GoogleId { get; set; }
        public string? FirebaseUid { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsPlatformAdmin { get; set; }
        public bool IsSuspended { get; set; }
        public bool HasGoogleIdentity { get; set; }
        public long? PlayerProfileId { get; set; }
    }
}
