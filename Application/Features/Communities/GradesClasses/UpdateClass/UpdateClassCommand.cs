using Application.Features.Communities.GradesClasses.Common;

using MediatR;

namespace Application.Features.Communities.GradesClasses.UpdateClass;

public class UpdateClassCommand : IRequest<ClassResponse>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public long ClassId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? GradeId { get; set; }
}
