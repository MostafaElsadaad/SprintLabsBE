namespace Application.Features.Communities.Teachers.ReplaceTeacherClassAssignments;

public class ReplaceTeacherClassAssignmentsRequest
{
    public List<long> ClassIds { get; set; } = new();
}
