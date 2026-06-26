using Application.Features.Communities.GradesClasses.Common;

using MediatR;

namespace Application.Features.Communities.GradesClasses.ListClasses;

public class ListClassesQuery : IRequest<List<ClassResponse>>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public long? GradeId { get; set; }
}
