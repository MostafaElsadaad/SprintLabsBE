namespace Domain.Services;

public interface ICommunityLoginActivationService
{
    Task ActivatePendingTeacherMembershipsAsync(long userId, CancellationToken cancellationToken);

    Task ActivatePendingStudentLicensesAsync(
        long userId,
        long playerProfileId,
        string email,
        CancellationToken cancellationToken);
}
