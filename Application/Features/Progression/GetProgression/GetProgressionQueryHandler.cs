using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.Progression.GetProgression;

public class GetProgressionQueryHandler : IRequestHandler<GetProgressionQuery, PlayerProgressionResponse>
{
    private readonly IProgressionReadService _service;
    public GetProgressionQueryHandler(IProgressionReadService service) => _service = service;
    public Task<PlayerProgressionResponse> Handle(GetProgressionQuery request, CancellationToken cancellationToken)
        => _service.GetProgressionAsync(request.UserId, request.PlayerProfileId, cancellationToken);
}
