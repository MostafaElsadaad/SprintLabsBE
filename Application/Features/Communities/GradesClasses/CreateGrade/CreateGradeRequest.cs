namespace Application.Features.Communities.GradesClasses.CreateGrade;

public class CreateGradeRequest
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
