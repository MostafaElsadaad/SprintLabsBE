namespace Application.Features.Communities.Teachers.Common;

public class TeacherResponse
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<TeacherClassResponse> Classes { get; set; } = new();
}
