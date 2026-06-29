using Application.Features.Communities.StudentLicenses.Common;

using Domain.Enums;

using MediatR;

namespace Application.Features.Communities.StudentLicenses.ListStudentLicenses;

public class ListStudentLicensesQuery : IRequest<List<StudentLicenseResponse>>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public StudentLicenseStatus? Status { get; set; }
    public long? GradeId { get; set; }
    public long? ClassId { get; set; }
    public string? Search { get; set; }
}
