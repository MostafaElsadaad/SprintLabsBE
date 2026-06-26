using Application.Features.Communities.GradesClasses.Common;

using MediatR;

namespace Application.Features.Communities.GradesClasses.CreateGrade;

public class CreateGradeCommand : IRequest<GradeResponse>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
