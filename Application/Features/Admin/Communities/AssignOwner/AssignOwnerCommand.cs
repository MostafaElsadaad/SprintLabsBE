using MediatR;

namespace Application.Features.Admin.Communities.AssignOwner;

public class AssignOwnerCommand : IRequest<OwnerResponse>
{
    public long AuthenticatedUserId { get; set; }
    public long CommunityId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
