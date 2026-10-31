using System.Net;
using Application.Features.Accounts.DevelopmentAuthentication.AssignDevelopmentSchool;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.DevelopmentAuthentication;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Requests;

namespace Application.Features.Accounts.DevelopmentAuthentication.SeedLeaderboardDemo;

// Administrative synthetic fixtures only, never a client match-result submission path.
public sealed class SeedLeaderboardDemoCommandHandler(
    IDevelopmentAuthenticationGuard guard, IUserService users, IPlayerRepository players,
    IBaseRepository<Community> communities, IBaseRepository<Grade> grades,
    IBaseRepository<Class> classes, IBaseRepository<Match> matches,
    IStudentEnrollmentTransaction transaction, IMediator mediator, IServiceScopeFactory scopes, TimeProvider? clock = null)
    : IRequestHandler<SeedLeaderboardDemoCommand, SeedLeaderboardDemoResponse>
{
    private const string Room = "development-leaderboard-demo-v1";

    public async Task<SeedLeaderboardDemoResponse> Handle(SeedLeaderboardDemoCommand request, CancellationToken ct)
    {
        guard.EnsureEndpointAccess(request.SuppliedApiKeys);
        var admin = await users.GetCurrentUser(request.AdminUserId);
        if (admin == null || !admin.IsPlatformAdmin || admin.IsSuspended || admin.IsLockedOut || admin.Status != "Active")
            throw Error(HttpStatusCode.Forbidden);
        if (request.CommunityId <= 0 || request.GradeId <= 0 || request.BaseClassId <= 0) throw Error(HttpStatusCode.BadRequest);
        if (!await communities.AsQueryable().AnyAsync(c => c.Id == request.CommunityId && c.Status == CommunityStatus.Active, ct) ||
            !await grades.AsQueryable().AnyAsync(g => g.Id == request.GradeId && g.CommunityId == request.CommunityId, ct) ||
            !await classes.AsQueryable().AnyAsync(c => c.Id == request.BaseClassId && c.CommunityId == request.CommunityId &&
                c.GradeId == request.GradeId && c.Status == ClassStatus.Active, ct)) throw Error(HttpStatusCode.NotFound);

        if (await classes.AsQueryable().AnyAsync(c => c.Id == request.BaseClassId &&
            (c.Name == "Demo Class B" || c.Name == "Demo Class C"), ct))
            throw Error(HttpStatusCode.BadRequest);
        // Validate the whole catalogue before creating fixtures. Never touch ordinary player identities.
        foreach (var definition in DevelopmentPlayerCatalog.All)
        {
            var user = await users.FindByEmail(definition.Email);
            if (user == null) throw Error(HttpStatusCode.NotFound);
            var player = await players.GetByUserIdAsync(user.Id);
            if (user.IsPlatformAdmin || user.IsTeacherAccount || user.IsSuspended || user.IsLockedOut || user.Status != "Active" ||
                !string.IsNullOrWhiteSpace(user.GoogleId) || !string.IsNullOrWhiteSpace(user.FirebaseUid) ||
                user.UserName != definition.AccountKey || !string.Equals(user.Email, definition.Email, StringComparison.OrdinalIgnoreCase) ||
                player == null || player.Id != user.PlayerProfileId || player.UserId != user.Id ||
                !string.IsNullOrWhiteSpace(player.GoogleId) || !string.Equals(player.Email, definition.Email, StringComparison.OrdinalIgnoreCase))
                throw Error(HttpStatusCode.Conflict);
        }

        // Serialize setup against enrollment capacity; assignment below reuses the existing seat logic.
        var classIds = await transaction.ExecuteAsync(request.CommunityId, async () =>
        {
            var ids = new List<long> { request.BaseClassId };
            for (int i = 2; i <= 3; i++)
            {
                string name = $"Demo Class {(char)('A' + i - 1)}";
                var existing = await classes.AsQueryable().SingleOrDefaultAsync(c => c.CommunityId == request.CommunityId &&
                    c.GradeId == request.GradeId && c.Name == name, ct);
                if (existing != null && existing.Status != ClassStatus.Active) throw Error(HttpStatusCode.Conflict);
                if (existing == null)
                {
                    existing = await classes.AddAsync(new Class { CommunityId = request.CommunityId, GradeId = request.GradeId, Name = name });
                    await classes.SaveChangesAsync();
                }
                ids.Add(existing.Id);
            }
            return ids;
        }, ct);

        var now = (clock ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        var year = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var month = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var week = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
        var response = new SeedLeaderboardDemoResponse { Scenario = $"leaderboard-v1-{week:yyyyMMdd}" };
        for (int i = 0; i < DevelopmentPlayerCatalog.All.Count; i++)
            response.Players.Add(await mediator.Send(new AssignDevelopmentSchoolCommand
            {
                AdminUserId = request.AdminUserId, AccountKey = DevelopmentPlayerCatalog.All[i].AccountKey,
                CommunityId = request.CommunityId, GradeId = request.GradeId, ClassId = classIds[i % 3],
                SuppliedApiKeys = request.SuppliedApiKeys
            }, ct));

        // Distinct winners and order per window. The server's normal reward calculator derives XP,
        // coins and RP from these explicit synthetic placements; no requested reward amounts exist.
        var roster = response.Players.Select(p => p.PlayerProfileId).ToList();
        var dates = new[] { year, month, week };
        for (int bucket = 0; bucket < dates.Length; bucket++)
        {
            // One unit of work per synthetic match: cached profile state from a previous completion
            // must not overwrite rewards committed concurrently by the real game server.
            using var scope = scopes.CreateScope();
            var progression = scope.ServiceProvider.GetRequiredService<IMatchProgressionService>();
            string code = $"dlb1-{request.CommunityId}-{request.GradeId}-{bucket}-{dates[bucket]:yyyyMMdd}";
            var registration = await progression.RegisterAsync(new RegisterMatchRequest
            {
                MatchCode = code, MirrorRoomId = Room, MatchType = "Ranked", CommunityId = request.CommunityId,
                StartedAt = dates[bucket].AddMinutes(-5), TotalPlayers = roster.Count, PlayerProfileIds = roster
            }, ct);
            var completion = new CompleteMatchRequest { MatchType = "Ranked", CommunityId = request.CommunityId,
                MirrorRoomId = Room, EndedAt = dates[bucket] };
            for (int i = 0; i < roster.Count; i++)
                completion.Players.Add(new MatchPlayerResultRequest { PlayerProfileId = roster[i],
                    Position = ((i - bucket * 3 + roster.Count) % roster.Count) + 1 });
            await progression.CompleteAsync(registration.MatchId, completion, ct);
            // Normal completion stamps the operator's current time. Only these catalogue-only,
            // stable-marked demo matches are dated into the requested calendar test windows.
            var match = await matches.AsQueryable().SingleAsync(m => m.Id == registration.MatchId, ct);
            if (match.MatchCode != code || match.MirrorRoomId != Room || match.Status != MatchStatus.Completed)
                throw Error(HttpStatusCode.Conflict);
            if (match.CompletedAt != dates[bucket])
            {
                match.CompletedAt = dates[bucket];
                await matches.UpdateAsync(match);
                await matches.SaveChangesAsync();
            }
            response.MatchIds.Add(match.Id);
        }
        return response;
    }

    private static GenericException Error(HttpStatusCode status) => new(ErrorCode.Failure, ErrorMessage.InvalidInput, status);
}