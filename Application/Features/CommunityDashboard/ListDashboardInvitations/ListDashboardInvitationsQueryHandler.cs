using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;

namespace Application.Features.CommunityDashboard.ListDashboardInvitations;

public class ListDashboardInvitationsQueryHandler : IRequestHandler<ListDashboardInvitationsQuery, List<InvitationView>>
{
    private readonly DashboardAuthorization _authorization;
    private readonly IBaseRepository<TeacherInvitation> _invitations;
    public ListDashboardInvitationsQueryHandler(DashboardAuthorization authorization, IBaseRepository<TeacherInvitation> invitations)
    { _authorization = authorization; _invitations = invitations; }

    public async Task<List<InvitationView>> Handle(ListDashboardInvitationsQuery request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, true, ct);
        var query = _invitations.AsQueryable().AsNoTracking().Where(x => x.CommunityUser.CommunityId == scope.CommunityId &&
            x.CommunityUser.Role == CommunityUserRole.Teacher && x.CommunityUser.Status == CommunityUserStatus.Pending &&
            x.AcceptedAt == null && x.RevokedAt == null);
        var now = DateTime.UtcNow;
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Select(x => new InvitationView { Id = x.Id, Email = x.InvitedEmail, SentAt = x.LastSentAt ?? x.CreatedAt,
                ExpiresAt = x.ExpiresAt, Status = x.ExpiresAt <= now ? "EXPIRED" : "PENDING" }).ToListAsync(ct);
        return items;
    }
}
