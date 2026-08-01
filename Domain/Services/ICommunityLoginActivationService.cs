namespace Domain.Services;

public interface ICommunityLoginActivationService
{
    Task ActivatePendingStudentLicensesAsync(
        long userId,
        long playerProfileId,
        string email,
        CancellationToken cancellationToken);

    Task ActivateEligiblePendingTeacherMembershipsAsync(
        long userId,
        string verifiedEmail,
        CancellationToken cancellationToken);
}
