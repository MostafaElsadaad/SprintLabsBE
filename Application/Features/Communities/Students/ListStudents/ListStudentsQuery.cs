using Application.Features.Communities.Students.Common;

using Domain.Enums;

using MediatR;

using Shared.Requests;
using Shared.Responses;

namespace Application.Features.Communities.Students.ListStudents;

public class ListStudentsQuery : PagedRequest, IRequest<PagedResponse<CommunityStudentListItemResponse>>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public long? GradeId { get; set; }
    public long? ClassId { get; set; }
    public StudentLicenseStatus? Status { get; set; }
    public string? Search { get; set; }
}
