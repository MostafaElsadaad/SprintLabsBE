namespace Shared.Responses;

public class TeacherInvitationClassResult
{
    public long ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public long GradeId { get; set; }
    public int Grade { get; set; }
}
