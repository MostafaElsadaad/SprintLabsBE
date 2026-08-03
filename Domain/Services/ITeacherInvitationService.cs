using Shared.Responses;

namespace Domain.Services;

public interface ITeacherInvitationService
{
    Task<TeacherInvitationIssueResult> IssueAsync(long invitedByUserId, long communityId, string email, CancellationToken cancellationToken);
    Task<TeacherInvitationValidationResult> ValidateAsync(string rawToken, CancellationToken cancellationToken);
    Task CompleteAsync(string rawToken, string name, string password, CancellationToken cancellationToken);
    Task RevokeForMembershipAsync(long communityUserId, CancellationToken cancellationToken);
}