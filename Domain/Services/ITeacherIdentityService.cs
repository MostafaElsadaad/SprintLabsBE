using Shared.Responses;

namespace Domain.Services;

public interface ITeacherIdentityService
{
    Task<TeacherRegistrationResult> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken);
    Task ConfirmEmailAsync(long userId, string token, CancellationToken cancellationToken);
    Task<ConfirmationDispatchResult> ResendConfirmationAsync(string email, CancellationToken cancellationToken);
    Task<TeacherIdentityResult> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);
    Task<PasswordResetDispatchResult> CreatePasswordResetAsync(string email, CancellationToken cancellationToken);
    Task ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken);
    Task<TeacherIdentityResult?> GetTeacherAsync(long userId, CancellationToken cancellationToken);
}
