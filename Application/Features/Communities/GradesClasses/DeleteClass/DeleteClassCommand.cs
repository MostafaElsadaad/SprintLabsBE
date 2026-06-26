using Application.Features.Communities.GradesClasses.Common;

using MediatR;

namespace Application.Features.Communities.GradesClasses.DeleteClass;

public class DeleteClassCommand : IRequest<ClassResponse>
{
    public long UserId { get; set; }
    public long CommunityId { get; set; }
    public long ClassId { get; set; }
}
