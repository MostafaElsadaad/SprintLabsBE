using MediatR;
using Shared.Requests;
using Shared.Responses;

namespace Application.Features.Progression.RegisterMatch;

public class RegisterMatchCommand : IRequest<MatchRegistrationResponse>
{
    public RegisterMatchRequest Request { get; set; } = new();
}
