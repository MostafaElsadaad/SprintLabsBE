namespace Application.Features.Communities.GradesClasses.Common;

public class ClassResponse
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
    public long GradeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
