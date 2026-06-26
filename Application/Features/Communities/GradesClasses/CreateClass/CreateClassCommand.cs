using Application.Features.Communities.GradesClasses.Common;

using MediatR;

namespace Application.Features.Communities.GradesClasses.CreateClass;

public class CreateClassCommand : IRequest<ClassResponse>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public long GradeId { get; set; }
    public string Name { get; set; } = string.Empty;
}
