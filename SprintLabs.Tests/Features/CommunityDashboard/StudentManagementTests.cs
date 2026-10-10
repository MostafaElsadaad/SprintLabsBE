using System.Text.Json;
using API.Controllers;
using Application.Features.CommunityDashboard.Common;
using Application.Features.CommunityDashboard.ListDashboardStudents;
using Domain.Enums;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using System.Text;
using System.Net;
using Application.Features.Communities.Students.StudentDetail;
using Application.Features.Communities.Students.StudentStats;
using Application.Features.Communities.Students.ImportStudents;
using Application.Features.Communities.Students.ExportStudents;
using Application.Features.Communities.StudentLicenses.AddStudentLicense;
using Application.Features.Communities.StudentLicenses.Common;
using Domain.Services;
using Infrastructure.Repositories;
using Infrastructure.Services;
using MediatR;
using Moq;
using Shared.Exceptions;

namespace Compass.Tests.Features.CommunityDashboard;

public class StudentManagementTests
{
    [Fact]
    public async Task ActiveFilter_ReturnsEnrollmentFieldsAndSearch()
    {
        using var f = new DashboardFixture();
        await f.SeedAsync();
        var page = await new ListDashboardStudentsQueryHandler(f.StudentRoster).Handle(
            new ListDashboardStudentsQuery { UserId = 10, Status = "ACTIVE", Search = "student1@" }, default);
        var row = JsonSerializer.SerializeToElement(DashboardResponseMapper.Student(Assert.Single(page.Data)));
        Assert.Equal("ACTIVE", row.GetProperty("Status").GetString());
        Assert.Equal("student1@example.com", row.GetProperty("Email").GetString());
        Assert.Equal("STU-000001", row.GetProperty("StudentCode").GetString());
        Assert.Equal(1, row.GetProperty("Class").GetProperty("Id").GetInt64());
        Assert.True(row.TryGetProperty("JoinedAt", out _));
    }

    [Fact]
    public void MissingStudentRoutes_ArePresentWithoutDuplicateRoster()
    {
        var routes = typeof(CommunitiesController).GetMethods().SelectMany(x => x.GetCustomAttributes(true)
            .OfType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>().Select(a => a.Template)).ToArray();
        Assert.Contains("students/stats", routes);
        Assert.Contains("students/import", routes);
        Assert.Contains("students/export", routes);
        Assert.Single(routes, x => x == "students");
    }

    [Fact]
    public async Task Stats_PartitionEnrollmentAndRestrictTeacherClasses()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        f.Context.StudentLicenses.AddRange(new StudentLicense { Id = 8, CommunityId = 1, ClassId = 1, GradeId = 107,
            Email = "pending@example.com", AssignedByUserId = 10 }, new StudentLicense { Id = 9, CommunityId = 1,
            ClassId = 2, GradeId = 108, Email = "removed@example.com", AssignedByUserId = 10, Status = StudentLicenseStatus.Revoked });
        await f.Context.SaveChangesAsync();
        var owner = await new StudentStatsQueryHandler(f.StudentRoster).Handle(new StudentStatsQuery { UserId = 10 }, default);
        Assert.Equal((4, 2, 1, 1), (owner.Total, owner.Active, owner.Inactive, owner.Pending));
        var teacher = await new StudentStatsQueryHandler(f.StudentRoster).Handle(new StudentStatsQuery { UserId = 20 }, default);
        Assert.Equal((2, 1, 0, 1), (teacher.Total, teacher.Active, teacher.Inactive, teacher.Pending));
        var revoked = await f.StudentRoster.ReadAsync(10, null, null, null, "inactive", default);
        Assert.Equal(9, Assert.Single(revoked).Id);
        var pending = await f.StudentRoster.ReadAsync(10, 1, 107, "PENDING@", "pending", default);
        Assert.Equal(8, Assert.Single(pending).Id);
        await Assert.ThrowsAsync<GenericException>(() => f.StudentRoster.ReadAsync(10, null, null, null, "UNKNOWN", default));
    }

    [Fact]
    public async Task Detail_LicenseIdDoesNotGuessPlayerIdAndHandlesPending()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        f.Context.StudentLicenses.Add(new StudentLicense { Id = 200, CommunityId = 1, ClassId = 1, GradeId = 107,
            Email = "pending@example.com", AssignedByUserId = 10 });
        f.Context.StudentLicenses.Single(x => x.Id == 1).PlayerProfileId = 2;
        await f.Context.SaveChangesAsync();
        var handler = new StudentDetailQueryHandler(f.StudentRoster, f.Authorization, f.Repo<Player>());
        var pending = JsonSerializer.SerializeToElement(await handler.Handle(new StudentDetailQuery { UserId = 10, StudentId = 200 }, default));
        Assert.Equal(JsonValueKind.Null, pending.GetProperty("PlayerProfileId").ValueKind);
        Assert.Equal("PENDING", pending.GetProperty("Status").GetString());
        Assert.Equal("pending@example.com", pending.GetProperty("Email").GetString());
        var detail = JsonSerializer.SerializeToElement(await handler.Handle(new StudentDetailQuery { UserId = 20, StudentId = 1 }, default));
        Assert.Equal(2, detail.GetProperty("PlayerProfileId").GetInt64());
        var error = await Assert.ThrowsAsync<GenericException>(() => handler.Handle(new StudentDetailQuery { UserId = 20, StudentId = 2 }, default));
        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        await Assert.ThrowsAsync<GenericException>(() => handler.Handle(new StudentDetailQuery { UserId = 10, StudentId = 1, IdType = "auto" }, default));
    }

    [Fact]
    public async Task Performance_UsesOnlyCompletedCommunityMatches()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        foreach (var id in new long[] { 1, 2, 3 }) f.Context.Matches.Add(new Domain.Models.Match { Id = id, MatchCode = "M" + id,
            CommunityId = id == 3 ? 2 : 1, Status = id == 2 ? MatchStatus.Started : MatchStatus.Completed,
            CompletedAt = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc) });
        f.Context.MatchPlayers.AddRange(Enumerable.Range(1, 3).Select(id => new MatchPlayer { MatchId = id,
            PlayerProfileId = 1, CommunityId = id == 3 ? 2 : 1, CorrectAnswers = id == 1 ? 3 : 100,
            WrongAnswers = id == 1 ? 1 : 0, IsWinner = true }));
        await f.Context.SaveChangesAsync();
        var row = Assert.Single(await f.StudentRoster.ReadAsync(20, 1, null, null, null, default));
        Assert.Equal(1, row.SessionsCount); Assert.Equal(75m, row.AvgScore);
        var detail = JsonSerializer.SerializeToElement(await new StudentDetailQueryHandler(f.StudentRoster, f.Authorization, f.Repo<Player>())
            .Handle(new StudentDetailQuery { UserId = 20, StudentId = 1 }, default));
        Assert.Equal(100m, detail.GetProperty("Analytics").GetProperty("WinRate").GetDecimal());
        Assert.Equal(1, detail.GetProperty("Analytics").GetProperty("MatchesPlayed").GetInt32());
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("xlsx")]
    public async Task Import_ReusesEnrollmentSkipsDuplicatesAndReportsRowsWithoutCreatingAccounts(string format)
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var capacity = f.Context.CommunityLicenses.Single(x => x.CommunityId == 1); capacity.MaxStudents = 3; capacity.UsedStudents = 2;
        await f.Context.SaveChangesAsync();
        var add = new AddStudentLicenseCommandHandler(f.Users.Object, new CommunityAccessService(f.Repo<CommunityUser>()),
            f.Repo<StudentLicense>(), f.Repo<CommunityLicense>(), f.Repo<Grade>(), f.Repo<Domain.Models.Class>(), f.Repo<CommunityUser>(),
            new PlayerRepository(f.Context), new StudentEnrollmentTransaction(f.Context));
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<IRequest<StudentLicenseResponse>>(), It.IsAny<CancellationToken>()))
            .Returns((IRequest<StudentLicenseResponse> command, CancellationToken ct) => add.Handle((AddStudentLicenseCommand)command, ct));
        var codec = new StudentRosterFileService();
        var bytes = codec.Write(new[] { "email", "gradeId", "classId" }, new List<string[]> {
            new[] { "new@example.com", "107", "1" }, new[] { "new@example.com", "107", "1" },
            new[] { "bad@example.com", "207", "3" }, new[] { "no-seat@example.com", "107", "1" } }, format);
        var handler = new ImportStudentsCommandHandler(f.Authorization, codec, f.Repo<StudentLicense>(), sender.Object);
        var request = new ImportStudentsCommand { UserId = 10, FileName = "students." + format, Bytes = bytes };
        var result = await handler.Handle(request, default);
        Assert.Equal((4, 1, 1, 2), (result.TotalRows, result.Imported, result.Skipped, result.Failed));
        Assert.Equal("NO_AVAILABLE_SEATS", result.Rows[3].ReasonCode);
        Assert.Equal(3, f.Context.CommunityLicenses.Single(x => x.CommunityId == 1).UsedStudents);
        Assert.Equal(5, f.Context.Users.Count());
        result = await handler.Handle(request, default);
        Assert.Equal((0, 2, 2), (result.Imported, result.Skipped, result.Failed));
        request.UserId = 20;
        var error = await Assert.ThrowsAsync<GenericException>(() => handler.Handle(request, default));
        Assert.Equal(HttpStatusCode.Forbidden, error.StatusCode);
    }

    [Theory]
    [InlineData("csv")]
    [InlineData("xlsx")]
    public async Task Export_HasOnlyPermittedRowsAndCanReimport(string format)
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var codec = new StudentRosterFileService();
        var download = await new ExportStudentsQueryHandler(f.StudentRoster, codec)
            .Handle(new ExportStudentsQuery { UserId = 20, Format = format, Status = "ACTIVE" }, default);
        var rows = codec.ReadImport(download.Bytes, download.FileName);
        Assert.Equal("student1@example.com", Assert.Single(rows).Email);
        Assert.DoesNotContain("student2@example.com", Encoding.UTF8.GetString(download.Bytes));
    }
}
