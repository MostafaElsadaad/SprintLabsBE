using Shared.Responses;

namespace Domain.Services;

public interface ITeacherIdentityService
{
    Task<TeacherIdentityResult> AuthenticateAsync(string identifier, string password, CancellationToken cancellationToken);
    Task<PasswordResetDispatchResult> CreatePasswordResetAsync(string identifier, CancellationToken cancellationToken);
    Task ResetPasswordAsync(long userId, string token, string newPassword, CancellationToken cancellationToken);
    Task<TeacherIdentityResult?> GetTeacherAsync(long userId, CancellationToken cancellationToken);
}