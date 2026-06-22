namespace Application.Features.Admin.Communities.Common;

public class CommunityLicenseSummaryResponse
{
    public int MaxStudents { get; set; }
    public int UsedStudents { get; set; }
    public int MaxTeachers { get; set; }
    public int UsedTeachers { get; set; }
    public int StudentEmailChangeLimit { get; set; }
}
