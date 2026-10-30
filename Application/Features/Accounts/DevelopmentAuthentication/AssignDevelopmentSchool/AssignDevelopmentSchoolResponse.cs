namespace Application.Features.Accounts.DevelopmentAuthentication.AssignDevelopmentSchool;
public sealed class AssignDevelopmentSchoolResponse
{
    public string AccountKey { get; set; } = "";
    public long PlayerProfileId { get; set; }
    public long CommunityId { get; set; }
    public long GradeId { get; set; }
    public long ClassId { get; set; }
    public long StudentLicenseId { get; set; }
    public bool AlreadyAssigned { get; set; }
}
