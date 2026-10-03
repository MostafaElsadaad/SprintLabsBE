using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Options;
using Shared.Responses;
using Application.Features.CommunityDashboard.Common;
using Application.Features.Accounts.TeacherAuthentication.Common;

namespace Application.Features.CommunityDashboard.MessageDashboardTeacher;

public class MessageDashboardTeacherCommandHandler : IRequestHandler<MessageDashboardTeacherCommand, long>
{
    private readonly DashboardAuthorization _authorization;
    private readonly IBaseRepository<CommunityUser> _members;
    private readonly IBaseRepository<Notification> _notifications;
    private readonly IBaseRepository<StaffActivity> _activities;

    public MessageDashboardTeacherCommandHandler(DashboardAuthorization authorization, IBaseRepository<CommunityUser> members, IBaseRepository<Notification> notifications, IBaseRepository<StaffActivity> activities)
    {
        _authorization = authorization;
        _members = members; _notifications = notifications; _activities = activities;
    }

    public async Task<long> Handle(MessageDashboardTeacherCommand request, CancellationToken ct)
    {
        var scope = await _authorization.ResolveAsync(request.UserId, true, ct);
        if (string.IsNullOrWhiteSpace(request.Body.Subject) || request.Body.Subject.Trim().Length > 200 ||
            string.IsNullOrWhiteSpace(request.Body.Body) || request.Body.Body.Trim().Length > 4000) throw DashboardAuthorization.Invalid();
        if (!await _members.AsQueryable().AnyAsync(x => x.CommunityId == scope.CommunityId &&
            x.UserId == request.TeacherId && x.Role == CommunityUserRole.Teacher && x.Status == CommunityUserStatus.Active, ct))
            throw DashboardAuthorization.NotFound();
        var now = DateTime.UtcNow;
        var notification = await _notifications.AddAsync(new Notification
        {
            CommunityId = scope.CommunityId, RecipientUserId = request.TeacherId, SenderUserId = request.UserId,
            Title = request.Body.Subject.Trim(), Body = request.Body.Body.Trim(), CreatedAt = now
        });
        await _activities.AddAsync(new StaffActivity { CommunityId = scope.CommunityId, TeacherUserId = request.TeacherId,
            ActorUserId = request.UserId, Label = "Received community message", CreatedAt = now });
        // Both base repositories share the scoped DbContext; one SaveChanges commits the two inserts atomically.
        await _notifications.SaveChangesAsync();
        return notification.Id;
    }
}
