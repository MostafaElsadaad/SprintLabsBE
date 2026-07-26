using Application.Features.Accounts.TeacherAuthentication.Common;

using Domain.Services;

using MediatR;

using Microsoft.Extensions.Options;

using Shared.Options;

namespace Application.Features.Accounts.TeacherAuthentication.RegisterTeacher;

public class RegisterTeacherCommandHandler : IRequestHandler<RegisterTeacherCommand, RegisterTeacherResponse>
{
    private readonly ITeacherIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly FrontendOptions _frontendOptions;

    public RegisterTeacherCommandHandler(ITeacherIdentityService identityService, IEmailService emailService, IOptions<FrontendOptions> frontendOptions)
    {
        _identityService = identityService;
        _emailService = emailService;
        _frontendOptions = frontendOptions.Value;
    }

    public async Task<RegisterTeacherResponse> Handle(RegisterTeacherCommand request, CancellationToken cancellationToken)
    {
        Validate(request.Name, request.Email, request.Password);
        var registration = await _identityService.RegisterAsync(request.Name, request.Email, request.Password, cancellationToken);
        await _emailService.SendConfirmationEmailAsync(registration.Email, registration.Name,
            TeacherAuthenticationLinkBuilder.Confirmation(_frontendOptions, registration.UserId, registration.ConfirmationToken), cancellationToken);
        return new RegisterTeacherResponse { ResendAvailableAt = registration.ResendAvailableAt };
    }

    private static void Validate(string name, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Name, email, and password are required.");
    }
}
