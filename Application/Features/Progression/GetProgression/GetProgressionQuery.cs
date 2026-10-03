using MediatR;
using Shared.Requests;
using Shared.Responses;

namespace Application.Features.Progression.GetProgression;

public class GetProgressionQuery : IRequest<PlayerProgressionResponse>
{
    public long UserId { get; set; }
    public long? PlayerProfileId { get; set; }
}
