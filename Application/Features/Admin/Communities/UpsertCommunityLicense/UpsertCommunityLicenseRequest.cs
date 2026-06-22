namespace Application.Features.Admin.Communities.UpsertCommunityLicense;

public class UpsertCommunityLicenseRequest
{
    public int MaxStudents { get; set; }
    public int MaxTeachers { get; set; }
    public int StudentEmailChangeLimit { get; set; }
}
