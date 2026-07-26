using MediatR;

namespace Application.Features.Accounts.TeacherAuthentication.ResendConfirmation;

public class ResendConfirmationCommand : IRequest<ResendConfirmationResponse>
{
    public string Email { get; set; } = string.Empty;
}
