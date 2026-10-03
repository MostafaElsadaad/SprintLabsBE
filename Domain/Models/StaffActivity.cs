namespace Domain.Models;

public class StaffActivity
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
    public long TeacherUserId { get; set; }
    public long ActorUserId { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
