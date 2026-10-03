using Domain.Services;
using MediatR;
using Shared.Responses;

namespace Application.Features.Progression.RegisterMatch;

public class RegisterMatchCommandHandler : IRequestHandler<RegisterMatchCommand, MatchRegistrationResponse>
{
    private readonly IMatchProgressionService _service;
    public RegisterMatchCommandHandler(IMatchProgressionService service) => _service = service;
    public Task<MatchRegistrationResponse> Handle(RegisterMatchCommand request, CancellationToken cancellationToken)
        => _service.RegisterAsync(request.Request, cancellationToken);
}
