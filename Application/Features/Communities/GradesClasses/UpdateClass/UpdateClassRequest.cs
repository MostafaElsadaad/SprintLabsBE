namespace Application.Features.Communities.GradesClasses.UpdateClass;

public class UpdateClassRequest
{
    public string Name { get; set; } = string.Empty;
    public long? GradeId { get; set; }
}
