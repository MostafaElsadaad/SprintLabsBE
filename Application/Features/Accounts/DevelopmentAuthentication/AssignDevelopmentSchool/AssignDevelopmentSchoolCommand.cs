using MediatR;
namespace Application.Features.Accounts.DevelopmentAuthentication.AssignDevelopmentSchool;
public sealed class AssignDevelopmentSchoolCommand : IRequest<AssignDevelopmentSchoolResponse>
{
    public long AdminUserId { get; set; }
    public string AccountKey { get; set; } = "";
    public long CommunityId { get; set; }
    public long GradeId { get; set; }
    public long ClassId { get; set; }
    public IReadOnlyCollection<string> SuppliedApiKeys { get; set; } = Array.Empty<string>();
}
