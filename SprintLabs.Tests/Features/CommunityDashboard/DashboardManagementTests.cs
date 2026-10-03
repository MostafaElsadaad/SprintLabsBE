using System.Net;
using Application.Features.CommunityDashboard.ListDashboardNotifications;
using Application.Features.CommunityDashboard.MessageDashboardTeacher;
using Domain.Enums;
using Domain.Models;
using Domain.Services;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Exceptions;
using Shared.Requests;

namespace Compass.Tests.Features.CommunityDashboard;

public class DashboardManagementTests
{
    [Fact]
    public async Task Class_assignment_preserves_multiple_teachers_and_unrelated_classes()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var service = new TeacherClassAssignmentService(f.Context);
        await service.AssignClassAsync(10, 1, 2, 20, default);
        await service.AssignClassAsync(10, 1, 2, 20, default);
        (await f.Context.TeacherClassAssignments.CountAsync()).Should().Be(3);
        (await f.Context.TeacherClassAssignments.CountAsync(x => x.ClassId == 1)).Should().Be(2);
        (await f.Context.StaffActivities.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Invalid_optional_teacher_does_not_persist_new_class()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var service = new TeacherClassAssignmentService(f.Context);
        await ((Func<Task>)(() => service.SaveClassAsync(10, 1, null, new DashboardClassRequest
            { Name = "New", GradeId = 107, TeacherId = 30 }, default))).Should().ThrowAsync<GenericException>();
        (await f.Context.Classes.CountAsync()).Should().Be(4);
    }

    [Fact]
    public async Task SaveClass_validates_grade_ownership_name_uniqueness_and_owner_role()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var service = new TeacherClassAssignmentService(f.Context);
        await ((Func<Task>)(() => service.SaveClassAsync(10, 1, null, new DashboardClassRequest
            { Name = "New", GradeId = 207 }, default))).Should().ThrowAsync<GenericException>();
        await ((Func<Task>)(() => service.SaveClassAsync(10, 1, null, new DashboardClassRequest
            { Name = "Alpha", GradeId = 107 }, default))).Should().ThrowAsync<GenericException>();
        await ((Func<Task>)(() => service.SaveClassAsync(20, 1, 1, new DashboardClassRequest
            { Name = "Renamed" }, default))).Should().ThrowAsync<GenericException>();
        var id = await service.SaveClassAsync(10, 1, null, new DashboardClassRequest
            { Name = "New", GradeId = 107, TeacherId = 20 }, default);
        (await f.Context.Classes.SingleAsync(x => x.Id == id)).Name.Should().Be("New");
        (await f.Context.TeacherClassAssignments.AnyAsync(x => x.ClassId == id && x.TeacherUserId == 20)).Should().BeTrue();
    }

    [Fact]
    public async Task Profile_updates_title_and_name_without_changing_login_identity()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var service = new TeacherProfileService(f.Context, f.UserManager());
        await service.UpdateAsync(10, 1, 20, "Updated", null, "LEAD_TEACHER", default);
        (await f.Context.Users.SingleAsync(x => x.Id == 20)).Name.Should().Be("Updated");
        (await f.Context.CommunityUsers.SingleAsync(x => x.UserId == 20)).TeacherTitle.Should().Be("LEAD_TEACHER");
        await ((Func<Task>)(() => service.UpdateAsync(10, 1, 20, null, "user21@example.com", null, default))).Should().ThrowAsync<GenericException>();
        await ((Func<Task>)(() => service.UpdateAsync(10, 1, 20, null, "newteacher@example.com", null, default))).Should().ThrowAsync<GenericException>();
        var user = await f.Context.Users.SingleAsync(x => x.Id == 20);
        user.Email.Should().Be("user20@example.com");
        user.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task Messages_and_unread_counts_are_recipient_isolated()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var handler = new MessageDashboardTeacherCommandHandler(f.Authorization, f.Repo<CommunityUser>(), f.Repo<Notification>(), f.Repo<StaffActivity>());
        var id = await handler.Handle(new MessageDashboardTeacherCommand { UserId = 10, TeacherId = 20,
            Body = new MessageDashboardTeacherRequest { Subject = "Hello", Body = "Class update" } }, default);
        (await f.Context.StaffActivities.CountAsync()).Should().Be(1);
        var list = new ListDashboardNotificationsQueryHandler(f.Authorization, f.Repo<Notification>());
        (await list.Handle(new ListDashboardNotificationsQuery { UserId = 20 }, default)).UnreadCount.Should().Be(1);
        (await list.Handle(new ListDashboardNotificationsQuery { UserId = 21 }, default)).Notifications.Should().BeEmpty();
        var recipient = await list.Handle(new ListDashboardNotificationsQuery { UserId = 20 }, default);
        recipient.Notifications.Single().Id.Should().Be(id);
        recipient.Notifications.Single().IsRead.Should().BeFalse();

    }

    [Fact]
    public async Task Message_rejects_non_teacher_and_invalid_body()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var handler = new MessageDashboardTeacherCommandHandler(f.Authorization, f.Repo<CommunityUser>(), f.Repo<Notification>(), f.Repo<StaffActivity>());
        await ((Func<Task>)(() => handler.Handle(new MessageDashboardTeacherCommand { UserId = 10, TeacherId = 30,
            Body = new MessageDashboardTeacherRequest { Subject = "Hello", Body = "Hidden" } }, default))).Should().ThrowAsync<GenericException>();
        await ((Func<Task>)(() => handler.Handle(new MessageDashboardTeacherCommand { UserId = 10, TeacherId = 20,
            Body = new MessageDashboardTeacherRequest { Subject = " ", Body = "Text" } }, default))).Should().ThrowAsync<GenericException>();
        (await f.Context.Notifications.CountAsync()).Should().Be(0);
    }
}
