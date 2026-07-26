
using Domain.Enums;
using Domain.Models;

using Microsoft.AspNetCore.Identity;

namespace Infrastructure.DataAccess
{
    public class User : IdentityUser<long>
    {
        public int? UserProfileId { get; set; }
        public string? GoogleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsPlatformAdmin { get; set; }
        public bool IsTeacherAccount { get; set; }
        public DateTime? LastConfirmationEmailSentAt { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Player? Player { get; set; }
        //public virtual UserProfile UserProfile { get; set; }

    }
}
