namespace Domain.Models;

public class TeacherClassAssignment
{
    public long Id { get; set; }
    public long TeacherUserId { get; set; }
    public long ClassId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Class Class { get; set; } = default!;
}
