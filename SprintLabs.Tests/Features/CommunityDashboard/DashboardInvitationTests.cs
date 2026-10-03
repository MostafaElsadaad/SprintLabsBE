using System.Net;
using Application.Features.CommunityDashboard.ListDashboardInvitations;
using Domain.Enums;
using Domain.Models;
using Domain.Services;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Exceptions;
using Shared.Options;

namespace Compass.Tests.Features.CommunityDashboard;

public class DashboardInvitationTests
{
    [Fact]
    public async Task Resend_invalidates_old_credential_and_preserves_pending_assignments()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var service = CreateService(f);
        var issue = await service.IssueAsync(10, 1, "invited@example.com", new long[] { 1 }, default);
        var old = await f.Context.TeacherInvitations.SingleAsync();
        old.LastSentAt = DateTime.UtcNow.AddMinutes(-2); await f.Context.SaveChangesAsync();
        await service.ResendAsync(10, 1, old.Id, default);
        old.RevokedAt.Should().NotBeNull();
        (await f.Context.TeacherClassAssignments.CountAsync(x => x.TeacherUserId == issue.UserId)).Should().Be(1);
        (await f.Context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedTeachers.Should().Be(3);
        await ((Func<Task>)(() => service.CancelAsync(10, 1, old.Id, default))).Should().ThrowAsync<GenericException>();
        (await f.Context.CommunityUsers.SingleAsync(x => x.UserId == issue.UserId)).Status.Should().Be(CommunityUserStatus.Pending);
    }

    [Fact]
    public async Task Cancellation_is_idempotent_and_releases_one_pending_seat()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var service = CreateService(f);
        var issue = await service.IssueAsync(10, 1, "invited@example.com", default);
        var invitation = await f.Context.TeacherInvitations.SingleAsync();
        await service.CancelAsync(10, 1, invitation.Id, default);
        await service.CancelAsync(10, 1, invitation.Id, default);
        (await f.Context.CommunityLicenses.SingleAsync(x => x.CommunityId == 1)).UsedTeachers.Should().Be(2);
        (await f.Context.CommunityUsers.SingleAsync(x => x.UserId == issue.UserId)).Status.Should().Be(CommunityUserStatus.Removed);
        (await new ListDashboardInvitationsQueryHandler(f.Authorization, f.Repo<TeacherInvitation>())
            .Handle(new ListDashboardInvitationsQuery { UserId = 10 }, default)).Should().BeEmpty();
    }

    [Fact]
    public async Task Resend_throttles_and_acceptance_cannot_be_cancelled()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var service = CreateService(f);
        await service.IssueAsync(10, 1, "invited@example.com", default);
        var invitation = await f.Context.TeacherInvitations.SingleAsync();
        var error = await ((Func<Task>)(() => service.ResendAsync(10, 1, invitation.Id, default))).Should().ThrowAsync<GenericException>();
        error.Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        invitation.AcceptedAt = DateTime.UtcNow; await f.Context.SaveChangesAsync();
        await ((Func<Task>)(() => service.CancelAsync(10, 1, invitation.Id, default))).Should().ThrowAsync<GenericException>();
    }

    [Fact]
    public async Task Invitation_lists_and_mutations_are_community_scoped()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var service = CreateService(f);
        await service.IssueAsync(10, 1, "invited@example.com", default);
        var invitation = await f.Context.TeacherInvitations.SingleAsync();
        invitation.ExpiresAt = DateTime.UtcNow.AddDays(-1); await f.Context.SaveChangesAsync();
        var list = new ListDashboardInvitationsQueryHandler(f.Authorization, f.Repo<TeacherInvitation>());
        (await list.Handle(new ListDashboardInvitationsQuery { UserId = 10 }, default)).Single().Status.Should().Be("EXPIRED");
        (await list.Handle(new ListDashboardInvitationsQuery { UserId = 30 }, default)).Should().BeEmpty();
        await ((Func<Task>)(() => service.CancelAsync(30, 2, invitation.Id, default))).Should().ThrowAsync<GenericException>();
        await ((Func<Task>)(() => service.ResendAsync(30, 2, invitation.Id, default))).Should().ThrowAsync<GenericException>();
    }

    private static TeacherInvitationService CreateService(DashboardFixture f) =>
        new(f.Context, f.UserManager(), Mock.Of<IRefreshTokenService>(), Options.Create(new TeacherAuthenticationOptions()));
}
