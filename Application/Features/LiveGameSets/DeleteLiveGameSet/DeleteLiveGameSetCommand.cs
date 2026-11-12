using MediatR;

namespace Application.Features.LiveGameSets.DeleteLiveGameSet;

public class DeleteLiveGameSetCommand : IRequest<bool>
{
    public long AuthenticatedUserId { get; set; }
    public long LiveGameSetId { get; set; }
}
