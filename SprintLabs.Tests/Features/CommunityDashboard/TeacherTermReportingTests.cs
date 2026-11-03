using System.Net;
using Application.Features.Communities.Common;
using Application.Features.Communities.UpdateCommunityProfile;
using Application.Features.CommunityDashboard.GetDashboardClass;
using Domain.Models;
using Infrastructure.Services;
using Shared.Exceptions;
using Shared.Requests;
using Shared.Responses;
using Xunit;

namespace Compass.Tests.Features.CommunityDashboard;

public class TeacherTermReportingTests
{
    [Theory]
    [InlineData("2026-10-31T23:59:59Z", 0)]
    [InlineData("2026-11-01T00:00:00Z", 0)]
    [InlineData("2026-11-01T12:00:00Z", 25)]
    [InlineData("2026-11-02T00:00:00Z", 50)]
    [InlineData("2026-11-03T00:00:00Z", 100)]
    [InlineData("2026-11-04T00:00:00Z", 100)]
    [InlineData("2026-11-02T02:00:00+02:00", 50)]
    public void CalendarProgress_UsesInclusiveEndUtcAndClamps(string date, decimal expected)
    {
        var term = new CommunityTermResponse { Name = "Term", StartDate = new(2026, 11, 1), EndDate = new(2026, 11, 2) };
        Assert.Equal(expected, CommunityTermPolicy.Progress(term, DateTimeOffset.Parse(date)));
    }

    [Fact]
    public void SameDayTerm_RoundsAndMissingOrInvalidLegacyDatesAreUnavailable()
    {
        var term = new CommunityTermResponse { Name = "Single day", StartDate = new(2026, 11, 1), EndDate = new(2026, 11, 1) };
        Assert.Equal(33.33m, CommunityTermPolicy.Progress(term, DateTimeOffset.Parse("2026-11-01T08:00:00Z")));
        Assert.Null(CommunityTermPolicy.Progress(null, DateTimeOffset.UtcNow));
        Assert.Null(CommunityTermPolicy.Read(new Community { TermName = "Partial", TermStartDate = new(2026, 11, 1) }));
        Assert.Null(CommunityTermPolicy.Read(new Community { TermName = "Reversed", TermStartDate = new(2026, 11, 2), TermEndDate = new(2026, 11, 1) }));
    }

    [Fact]
    public async Task ProfileTerm_UpdateValidatePreserveClearAndRejectTeacher()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var handler = new UpdateCommunityProfileCommandHandler(f.Users.Object, new CommunityAccessService(f.Repo<CommunityUser>()), f.Repo<Community>());
        var input = new UpdateCommunityProfileCommand { UserId = 10, CommunityId = 1, Name = "School One", Slug = "one",
            Term = new CommunityTermRequest { Name = "  Sample term  ", StartDate = new(2026, 9, 1), EndDate = new(2026, 12, 31) } };
        var result = await handler.Handle(input, default);
        Assert.Equal("Sample term", result.Term!.Name);
        input.Term = null;
        Assert.Equal("Sample term", (await handler.Handle(input, default)).Term!.Name);
        input.Term = new() { Name = "No dates" };
        var error = await Assert.ThrowsAsync<GenericException>(() => handler.Handle(input, default));
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Equal("Sample term", f.Context.Communities.Single(x => x.Id == 1).TermName);
        input.Term = new() { Name = "Bad dates", StartDate = new(2026, 11, 2), EndDate = new(2026, 11, 1) };
        await Assert.ThrowsAsync<GenericException>(() => handler.Handle(input, default));
        input.Term = new() { Name = " ", StartDate = new(2026, 11, 1), EndDate = new(2026, 11, 2) };
        await Assert.ThrowsAsync<GenericException>(() => handler.Handle(input, default));
        input.Term = new() { Name = new string('a', 101), StartDate = new(2026, 11, 1), EndDate = new(2026, 11, 2) };
        await Assert.ThrowsAsync<GenericException>(() => handler.Handle(input, default));
        input.Term = new(); input.UserId = 20;
        error = await Assert.ThrowsAsync<GenericException>(() => handler.Handle(input, default));
        Assert.Equal(HttpStatusCode.Forbidden, error.StatusCode);
        input.UserId = 10;
        Assert.Null((await handler.Handle(input, default)).Term);
    }

    [Fact]
    public async Task ClassProjection_UsesOwnCommunityTermAndPreservesAssignedClassScope()
    {
        using var f = new DashboardFixture(); await f.SeedAsync();
        var own = f.Context.Communities.Single(x => x.Id == 1);
        CommunityTermPolicy.Apply(own, new() { Name = "Own term", StartDate = new(2000, 1, 1), EndDate = new(2000, 2, 1) });
        var other = f.Context.Communities.Single(x => x.Id == 2);
        CommunityTermPolicy.Apply(other, new() { Name = "Other term", StartDate = new(2099, 1, 1), EndDate = new(2099, 2, 1) });
        await f.Context.SaveChangesAsync();
        var scope = await f.Authorization.ResolveAsync(20, false, default);
        var row = Assert.Single(await f.Projection.ClassesAsync(scope, default));
        Assert.Equal("Own term", row.Term!.Name); Assert.Equal(100m, row.TermProgress);
        var handler = new GetDashboardClassQueryHandler(f.Authorization, f.Projection);
        Assert.Equal("Own term", (await handler.Handle(new() { UserId = 20, ClassId = 1 }, default)).Term!.Name);
        var error = await Assert.ThrowsAsync<GenericException>(() => handler.Handle(new() { UserId = 20, ClassId = 3 }, default));
        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        Assert.Equal("Other term", Assert.Single(await f.Projection.ClassesAsync(await f.Authorization.ResolveAsync(30, false, default), default)).Term!.Name);
    }
}
