namespace Application.Features.Communities.Teachers.Common;

public class TeacherClassResponse
{
    public long ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public long GradeId { get; set; }
    public int Grade { get; set; }
}
