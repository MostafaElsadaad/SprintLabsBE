using Application.Features.Communities.GradesClasses.Common;

using MediatR;

namespace Application.Features.Communities.GradesClasses.ListGrades;

public class ListGradesQuery : IRequest<List<GradeResponse>>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
}
