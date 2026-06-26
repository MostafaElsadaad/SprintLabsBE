namespace Application.Features.Communities.GradesClasses.CreateClass;

public class CreateClassRequest
{
    public long GradeId { get; set; }
    public string Name { get; set; } = string.Empty;
}
