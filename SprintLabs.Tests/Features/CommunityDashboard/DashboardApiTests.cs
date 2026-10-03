using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using API.Controllers;
using Application;
using Application.Features.CommunityDashboard.Common;
using Asp.Versioning;
using Domain.Repositories;
using Domain.Services;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Exceptions;
using Shared.Responses;

namespace Compass.Tests.Features.CommunityDashboard;

public class DashboardApiTests
{
    [Fact]
    public async Task Http_routes_ignore_supplied_identity_and_enforce_assigned_classes()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        using var server = Server(f);
        using var client = server.CreateClient();
        (await client.GetAsync("/api/v1/Communities/classes")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Add("X-Test-User", "20");
        var response = await Data(client, "/api/v1/Communities/classes?userId=10&page=1&pageSize=10");
        response.GetProperty("total").GetInt32().Should().Be(1);
        response.GetProperty("items")[0].GetProperty("id").GetInt64().Should().Be(1);
        (await client.GetAsync("/api/v1/Communities/classes/2?userId=10")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync("/api/v1/Communities/teachers")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/v1/Communities/classes?pageSize=101")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Owner_management_and_notification_http_contracts_work()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        using var server = Server(f);
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "10");
        var created = await client.PostAsJsonAsync("/api/v1/Communities/classes",
            new { name = "Gamma", gradeId = 107, teacherId = 20 });
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var row = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        row.GetProperty("name").GetString().Should().Be("Gamma");
        row.GetProperty("teachers").GetArrayLength().Should().Be(1);
        row.GetProperty("teachers")[0].GetProperty("id").GetInt64().Should().Be(20);
        var message = await client.PostAsJsonAsync("/api/v1/Communities/teachers/20/message",
            new { subject = "Notice", body = "Hello" });
        message.StatusCode.Should().Be(HttpStatusCode.OK);
        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Add("X-Test-User", "20");
        var identity = await client.GetFromJsonAsync<BaseResponse<Application.Features.CommunityDashboard.StaffIdentity.StaffIdentityResponse>>(
            "/api/v1/CommunityDashboard/me");
        identity!.Data.Role.Should().Be("TEACHER");
        identity.Data.UnreadNotificationsCount.Should().Be(1);
    }

    [Fact]
    public async Task Teacher_dashboard_matches_requested_fields_with_zero_placeholders_and_assigned_classes()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        using var server = Server(f); using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "20");
        var row = await Data(client, "/api/v1/CommunityDashboard/dashboard");
        Keys(row, "classesCount", "studentsCount", "sessionsThisWeek", "sessionsChangeVsLastWeekPercent",
            "gameplayHoursThisWeek", "avgHoursPerStudent", "classes");
        row.GetProperty("classesCount").GetInt32().Should().Be(1);
        row.GetProperty("studentsCount").GetInt32().Should().Be(1);
        foreach (var field in new[] { "sessionsThisWeek", "sessionsChangeVsLastWeekPercent", "gameplayHoursThisWeek", "avgHoursPerStudent" })
            row.GetProperty(field).GetDecimal().Should().Be(0);
        var classes = row.GetProperty("classes");
        classes.GetArrayLength().Should().Be(1);
        Keys(classes[0], "id", "name", "grade", "studentsCount", "nextSession");
        classes[0].GetProperty("grade").GetInt32().Should().Be(7);
        classes[0].GetProperty("nextSession").ValueKind.Should().Be(JsonValueKind.Null);
        var detail = await Data(client, "/api/v1/Communities/classes/1");
        Keys(detail, "id", "name", "grade", "teacherName", "createdAt", "studentsCount", "avgScore", "inactiveStudentsCount");
        detail.GetProperty("teacherName").GetString().Should().Be("User 20");
        detail.GetProperty("avgScore").GetDecimal().Should().Be(0);
        detail.GetProperty("inactiveStudentsCount").GetInt32().Should().Be(0);
        var identity = await Data(client, "/api/v1/CommunityDashboard/me");
        Keys(identity, "id", "fullName", "role", "title", "unreadNotificationsCount");
        identity.GetProperty("title").GetString().Should().Be("Grade 7");
    }

    [Fact]
    public async Task Owner_reads_match_frontend_shapes_and_retain_multiple_teachers()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        using var server = Server(f); using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "10");
        var dashboard = await Data(client, "/api/v1/CommunityDashboard/dashboard");
        Keys(dashboard, "totalStudents", "newStudentsThisWeek", "activeClasses", "gradesCount", "teachersCount",
            "pendingInvitationsCount", "gameplayHours", "gameplayHoursChangeVsPreviousMonthPercent");
        dashboard.GetProperty("totalStudents").GetInt32().Should().Be(2);
        dashboard.GetProperty("gameplayHours").GetDecimal().Should().Be(0);
        var page = await Data(client, "/api/v1/Communities/classes?page=1&pageSize=1");
        Keys(page, "items", "page", "pageSize", "total");
        page.GetProperty("total").GetInt32().Should().Be(2);
        var row = page.GetProperty("items")[0];
        Keys(row, "id", "name", "grade", "teachers", "studentsCount", "status");
        Keys(row.GetProperty("grade"), "id", "name");
        row.GetProperty("teachers").GetArrayLength().Should().Be(2);
        var secondPage = await Data(client, "/api/v1/Communities/classes?page=2&pageSize=1");
        secondPage.GetProperty("items")[0].GetProperty("id").GetInt64().Should().Be(2);
        var detail = await Data(client, "/api/v1/Communities/classes/1");
        Keys(detail, "id", "name", "grade", "status", "teachers", "studentsCount", "avgScore", "createdAt", "lastActiveAt", "studentsPreview");
        Keys(detail.GetProperty("studentsPreview")[0], "id", "fullName");
        var grades = await Data(client, "/api/v1/Communities/grades");
        grades.GetArrayLength().Should().Be(6);
        Keys(grades[0], "id", "name", "classesCount", "studentsCount");
        var students = await Data(client, "/api/v1/Communities/students?classId=1&page=1");
        Keys(students, "items", "page", "pageSize", "total");
        Keys(students.GetProperty("items")[0], "id", "fullName", "avgScore", "sessionsCount", "status");
        students.GetProperty("items")[0].GetProperty("sessionsCount").GetInt32().Should().Be(0);
        var teachers = await Data(client, "/api/v1/Communities/teachers");
        Keys(teachers.GetProperty("items")[0], "id", "fullName", "title", "teacherCode", "grade", "classes", "studentsCount", "joinedAt", "status");
        Keys(teachers.GetProperty("items")[0].GetProperty("classes")[0], "id", "name");
        var teacher = await Data(client, "/api/v1/Communities/teachers/20");
        Keys(teacher, "id", "fullName", "teacherCode", "title", "status", "email", "grade", "studentsCount", "joinedAt", "lastActiveAt", "licenseStatus", "classes", "recentActivity");
        var activity = await Data(client, "/api/v1/Communities/teachers/activity");
        Keys(activity.GetProperty("items")[0], "teacherId", "fullName", "classesCount", "totalSessions", "lastPlayedAt");
        activity.GetProperty("items")[0].GetProperty("totalSessions").GetInt32().Should().Be(0);
        (await Data(client, "/api/v1/Communities/invitations")).ValueKind.Should().Be(JsonValueKind.Array);
        var notifications = await Data(client, "/api/v1/CommunityDashboard/notifications");
        Keys(notifications, "notifications", "unreadCount");
        notifications.GetProperty("notifications").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task Duplicate_dashboard_routes_are_removed_and_existing_class_mutations_are_reused()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        using var server = Server(f); using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "10");
        foreach (var path in new[] { "classes", "grades", "teachers", "invitations", "classes/1/students" })
            (await client.GetAsync("/api/v1/CommunityDashboard/" + path)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsync("/api/v1/CommunityDashboard/classes/1/archive", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsync("/api/v1/CommunityDashboard/teachers/20/deactivate", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsync("/api/v1/CommunityDashboard/notifications/1/read", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var assigned = await client.PostAsJsonAsync("/api/v1/Communities/classes/2/assign-teacher", new { teacherId = 20 });
        assigned.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await client.PatchAsJsonAsync("/api/v1/Communities/classes/2", new { name = "Beta updated" });
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Data(client, "/api/v1/Communities/classes/2")).GetProperty("name").GetString().Should().Be("Beta updated");
        (await client.DeleteAsync("/api/v1/Communities/classes/2")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Data(client, "/api/v1/Communities/classes?status=ARCHIVED")).GetProperty("total").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Teacher_cannot_bypass_class_scope_using_existing_students_route()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        using var server = Server(f); using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "20");
        var students = await Data(client, "/api/v1/Communities/students?userId=10");
        students.GetProperty("total").GetInt32().Should().Be(1);
        (await client.GetAsync("/api/v1/Communities/students?classId=2")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync("/api/v1/Communities/classes/3")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync("/api/v1/Communities/classes", new { name = "Hidden", gradeId = 107 })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/v1/Communities/teachers/stats")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static void Keys(JsonElement row, params string[] expected) =>
        row.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo(expected);

    private static async Task<JsonElement> Data(HttpClient client, string route)
    {
        using var response = await client.GetAsync(route);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").Clone();
    }

    private static TestServer Server(DashboardFixture f) => new(new WebHostBuilder()
        .ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddSingleton(f.Context);
            services.AddSingleton(f.Users.Object);
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddScoped<ICommunityAccessService, CommunityAccessService>();
            services.AddScoped<ITeacherClassAssignmentService, TeacherClassAssignmentService>();
            services.AddApplicationServices(new ConfigurationBuilder().Build());
            services.AddControllers().AddApplicationPart(typeof(CommunityDashboardController).Assembly);
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            }).AddMvc();
            services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("test", _ => { });
            services.AddAuthorization();
        })
        .Configure(app =>
        {
            app.Use(async (context, next) =>
            {
                try { await next(); }
                catch (GenericException error) { context.Response.StatusCode = (int)(error.StatusCode ?? HttpStatusCode.InternalServerError); }
            });
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }));

    private sealed class TestAuthentication : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder) { }
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!long.TryParse(Request.Headers["X-Test-User"], out var id)) return Task.FromResult(AuthenticateResult.NoResult());
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", id.ToString()) }, "test"));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "test")));
        }
    }
}
