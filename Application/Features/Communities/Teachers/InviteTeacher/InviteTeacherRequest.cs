namespace Application.Features.Communities.Teachers.InviteTeacher;

public class InviteTeacherRequest
{
    public string Email { get; set; } = string.Empty;
    public List<long> ClassIds { get; set; } = new();
}
