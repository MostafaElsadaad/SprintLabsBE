namespace Application.Features.Communities.GradesClasses.Common;

public class GradeResponse
{
    public long Id { get; set; }
    public long CommunityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int ClassCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
