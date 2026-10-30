namespace Application.Features.Accounts.DevelopmentAuthentication.AssignDevelopmentSchool;
public sealed class AssignDevelopmentSchoolRequest
{
    public long CommunityId { get; set; }
    public long GradeId { get; set; }
    public long ClassId { get; set; }
}
