using Application.Features.CommunityDashboard.Common;
using Application.Features.Communities.Students.Common;
using Domain.Models;
using Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Communities.Students.StudentDetail;

public class StudentDetailQueryHandler(StudentRosterService roster, DashboardAuthorization authorization,
    IBaseRepository<Player> players) : IRequestHandler<StudentDetailQuery, object>
{
    public async Task<object> Handle(StudentDetailQuery request, CancellationToken ct)
    {
        if (request.StudentId <= 0 || request.IdType is not ("license" or "playerProfile")) throw DashboardAuthorization.Invalid();
        var rows = await roster.ReadAsync(request.UserId, null, null, null, null, ct);
        var row = request.IdType == "license" ? rows.SingleOrDefault(x => x.Id == request.StudentId) :
            rows.Where(x => x.PlayerProfileId == request.StudentId).OrderBy(x => x.Status == "INACTIVE").ThenByDescending(x => x.JoinedAt).FirstOrDefault();
        if (row == null) throw DashboardAuthorization.NotFound();
        var scope = await authorization.ResolveAsync(request.UserId, false, ct);
        var performance = await roster.PerformanceAsync(scope.CommunityId, new[] { row.PlayerProfileId }.OfType<long>(), ct);
        var analytics = row.PlayerProfileId.HasValue ? performance.GetValueOrDefault(row.PlayerProfileId.Value) ?? new() : new StudentPerformance();
        var player = row.PlayerProfileId.HasValue ? await players.AsQueryable().AsNoTracking().SingleOrDefaultAsync(x => x.Id == row.PlayerProfileId.Value, ct) : null;
        return new { row.Id, row.StudentCode, row.UserId, row.PlayerProfileId, Name = row.FullName, row.FullName, row.Email,
            row.Status, LicenseStatus = row.LicenseStatus.ToUpperInvariant(), Class = new { Id = row.ClassId, Name = row.ClassName }, row.Grade,
            row.ClassId, row.ClassName, GradeId = row.Grade.Id, GradeName = row.Grade.Name, row.JoinedAt, row.ActivatedAt, CreatedAt = row.JoinedAt,
            AvatarUrl = player?.AvatarUrl, Gold = player?.Gold ?? 0, Experience = player?.Experience ?? 0, Level = player?.Level ?? 0,
            Analytics = new { CompletedAssignments = 0, analytics.MatchesPlayed, SessionsCount = analytics.MatchesPlayed,
                analytics.AverageScore, analytics.WinRate, analytics.Wins, analytics.CorrectAnswers, analytics.WrongAnswers, analytics.LastActivityAt } };
    }
}
