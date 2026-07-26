using Shared.Responses;

namespace Domain.Services;

public interface ITeacherInvitationService
{
    Task<TeacherInvitationIssueResult> IssueAsync(long invitedByUserId, long communityId, string email, string name, CancellationToken cancellationToken);
    Task<TeacherInvitationAcceptanceResponse> AcceptAsync(long userId, string rawToken, CancellationToken cancellationToken);
    Task RevokeForMembershipAsync(long communityUserId, CancellationToken cancellationToken);
}
