using System.Net;
using Application.Features.CommunityDashboard.Common;
using Application.Features.CommunityDashboard.GetDashboardClass;
using Application.Features.CommunityDashboard.ListDashboardClasses;
using Application.Features.CommunityDashboard.ListDashboardGrades;
using Application.Features.CommunityDashboard.ListDashboardStudents;
using Application.Features.CommunityDashboard.SearchDashboard;
using Application.Features.CommunityDashboard.StaffDashboard;
using Domain.Enums;
using Domain.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace Compass.Tests.Features.CommunityDashboard;

public class DashboardReadTests
{
    [Fact]
    public async Task Teacher_sees_only_assigned_class_and_students()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var scope = await f.Authorization.ResolveAsync(20, false, default);
        scope.ClassIds.Should().Equal(1);
        var classes = await new ListDashboardClassesQueryHandler(f.Authorization, f.Projection)
            .Handle(new ListDashboardClassesQuery { UserId = 20 }, default);
        classes.TotalRecords.Should().Be(1);
        classes.Data.Single().Teachers.Should().HaveCount(2);
        var students = await new ListDashboardStudentsQueryHandler(f.StudentRoster)
            .Handle(new ListDashboardStudentsQuery { UserId = 20, ClassId = 1 }, default);
        students.Data.Single().FullName.Should().Be("Student 1");
        students.Data.Single().SessionsCount.Should().Be(0);
        students.Data.Single().ActivityStatus.Should().Be("UNKNOWN");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Teacher_cannot_read_unassigned_foreign_or_archived_class(long id)
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var action = () => new GetDashboardClassQueryHandler(f.Authorization, f.Projection)
            .Handle(new GetDashboardClassQuery { UserId = 20, ClassId = id }, default);
        (await action.Should().ThrowAsync<GenericException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Owner_list_filters_sort_and_pages_without_other_community()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var handler = new ListDashboardClassesQueryHandler(f.Authorization, f.Projection);
        var page = await handler.Handle(new ListDashboardClassesQuery { UserId = 10, Sort = "name:desc", PageSize = 1 }, default);
        page.TotalRecords.Should().Be(2);
        page.Data.Single().Name.Should().Be("Beta");
        var missing = await handler.Handle(new ListDashboardClassesQuery { UserId = 10, Status = "NO_TEACHER" }, default);
        missing.Data.Single().Id.Should().Be(2);
    }

    [Fact]
    public async Task Search_and_grades_are_scoped_and_omit_progress_percentages()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var results = await new SearchDashboardQueryHandler(f.Authorization, f.Projection)
            .Handle(new SearchDashboardQuery { UserId = 20, Q = "Student" }, default);
        results.Should().ContainSingle().Which.Id.Should().Be(1);
        var grades = await new ListDashboardGradesQueryHandler(f.Authorization, f.Projection, f.Repo<Grade>())
            .Handle(new ListDashboardGradesQuery { UserId = 10 }, default);
        grades.Select(x => x.Value).Should().Equal(7, 8, 9, 10, 11, 12);
        grades[0].StudentsCount.Should().Be(1);
    }

    [Fact]
    public async Task Dashboard_counts_current_roster_without_fabricating_gameplay()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var row = await new StaffDashboardQueryHandler(f.Authorization, f.Projection, f.Repo<TeacherInvitation>(), f.Repo<StudentLicense>())
            .Handle(new StaffDashboardQuery { UserId = 10 }, default);
        row.TotalStudents.Should().Be(2);
        row.ActiveClasses.Should().Be(2);
        row.TeachersCount.Should().Be(2);
        row.GameplayMetricsAvailable.Should().BeFalse();
        row.GameplayHours.Should().BeNull();
        row.SessionsThisWeek.Should().BeNull();
    }

    [Fact]
    public async Task Dashboard_handles_empty_roster_without_reporting_missing_data_as_zero_gameplay()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        f.Context.StudentLicenses.RemoveRange(f.Context.StudentLicenses);
        f.Context.TeacherClassAssignments.RemoveRange(f.Context.TeacherClassAssignments);
        f.Context.Classes.RemoveRange(f.Context.Classes);
        await f.Context.SaveChangesAsync();
        var row = await new StaffDashboardQueryHandler(f.Authorization, f.Projection, f.Repo<TeacherInvitation>(), f.Repo<StudentLicense>())
            .Handle(new StaffDashboardQuery { UserId = 10 }, default);
        row.TotalStudents.Should().Be(0);
        row.ActiveClasses.Should().Be(0);
        row.Classes.Should().BeEmpty();
        row.GameplayHours.Should().BeNull();
        row.GameplayMetricsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Suspended_pending_removed_or_ambiguous_context_is_rejected()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var teacher = await f.Context.Users.SingleAsync(x => x.Id == 20);
        teacher.Status = UserStatus.Suspended; await f.Context.SaveChangesAsync();
        await ((Func<Task>)(() => f.Authorization.ResolveAsync(20, false, default))).Should().ThrowAsync<GenericException>();
        await ((Func<Task>)(() => f.Authorization.ResolveAsync(40, false, default))).Should().ThrowAsync<GenericException>();
        f.Context.CommunityUsers.Add(new CommunityUser { UserId = 10, CommunityId = 2,
            Role = CommunityUserRole.Owner, Status = CommunityUserStatus.Active });
        await f.Context.SaveChangesAsync();
        await ((Func<Task>)(() => f.Authorization.ResolveAsync(10, false, default))).Should().ThrowAsync<GenericException>();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public void Paging_rejects_invalid_bounds_and_overflow(int page, int size) =>
        ((Action)(() => DashboardPaging.Validate(page, size))).Should().Throw<GenericException>();

    [Fact]
    public void Monday_week_boundary_and_invalid_ranges_are_deterministic()
    {
        DashboardPeriod.WeekStart(new DateTime(2026, 10, 4)).Should().Be(new DateTime(2026, 9, 28));
        DashboardPeriod.WeekStart(new DateTime(2026, 10, 5)).Should().Be(new DateTime(2026, 10, 5));
        ((Action)(() => DashboardPeriod.Range(new DateTime(2026, 10, 4), new DateTime(2026, 10, 1))))
            .Should().Throw<GenericException>();
    }
}
