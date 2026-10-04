using System.Net;
using Domain.Enums;
using Domain.Models;
using Domain.Services;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Requests;
using Shared.Responses;

namespace Infrastructure.Services;

public partial class ProgressionReadService : IProgressionReadService
{
    private readonly ApplicationDbContext _context;
    private readonly ILevelProgressionService _levels;
    private readonly ICommunityAccessService _access;
    private readonly TimeProvider _clock;

    public ProgressionReadService(ApplicationDbContext context, ILevelProgressionService levels, ICommunityAccessService access, TimeProvider? clock = null)
    { _context = context; _levels = levels; _access = access; _clock = clock ?? TimeProvider.System; }

    public async Task<PlayerProgressionResponse> GetProgressionAsync(long userId, long? playerId, CancellationToken ct)
    {
        var user = await CallerAsync(userId, ct);
        var player = await AccessiblePlayerAsync(user, playerId, ct);
        var level = _levels.CalculateProgression(new LevelProgressionRequest { OldTotalXp = player.Experience });
        var remaining = player.Experience;
        for (var i = 1; i < level.NewLevel; i++) remaining -= _levels.CalculateXpRequiredForLevel(i);
        return new() { PlayerProfileId = player.Id, Experience = player.Experience, Level = level.NewLevel,
            Gold = player.Gold, Rp = player.Rp, RankTier = player.RankTier.ToString(), HighestRankTier = player.HighestRankTier.ToString(),
            TotalMatches = player.TotalMatches, TotalWins = player.TotalWins,
            XpIntoCurrentLevel = remaining, NextLevelXpRequirement = level.NextLevelXpRequirement };
    }

    public async Task<PlayerRankingResponse> GetRankingAsync(long userId, long? playerId, CancellationToken ct)
    {
        var player = await AccessiblePlayerAsync(await CallerAsync(userId, ct), playerId, ct);
        return new() { PlayerProfileId = player.Id, Rp = player.Rp,
            RankTier = player.RankTier.ToString(), HighestRankTier = player.HighestRankTier.ToString() };
    }

    public async Task<ProgressionPage<MatchHistoryResponse>> GetHistoryAsync(long userId, long? communityId,
        long? playerId, ProgressionPageRequest page, CancellationToken ct)
    {
        ValidatePage(page);
        if (page.GradeId.HasValue || page.ClassId.HasValue || playerId is <= 0) throw Invalid();
        var user = await CallerAsync(userId, ct);
        var matches = _context.Matches.AsNoTracking().Where(x => x.Status == MatchStatus.Completed);
        long? selectedPlayer;
        if (communityId.HasValue)
        {
            var licenses = await ScopedLicensesAsync(user, communityId.Value, ct);
            matches = matches.Where(x => x.CommunityId == communityId);
            var wholeCommunity = user.IsPlatformAdmin ||
                await _access.HasCommunityRole(user.Id, communityId.Value, new[] { CommunityUserRole.Owner });
            if (!wholeCommunity)
                matches = matches.Where(x => x.MatchPlayers.Any(p => licenses.Any(l => l.PlayerProfileId == p.PlayerProfileId)));
            selectedPlayer = playerId;
            if (selectedPlayer.HasValue)
            {
                if (!await licenses.AnyAsync(x => x.PlayerProfileId == selectedPlayer, ct) &&
                    !(wholeCommunity && await matches.AnyAsync(x => x.MatchPlayers.Any(p => p.PlayerProfileId == selectedPlayer), ct))) throw NotFound();
                matches = matches.Where(x => x.MatchPlayers.Any(p => p.PlayerProfileId == selectedPlayer));
            }
        }
        else
        {
            selectedPlayer = (await AccessiblePlayerAsync(user, null, ct)).Id;
            matches = matches.Where(x => x.MatchPlayers.Any(p => p.PlayerProfileId == selectedPlayer));
        }
        var total = await matches.CountAsync(ct);
        var rows = await matches.Include(x => x.MatchPlayers).Include(x => x.MatchRewardResults).AsSplitQuery()
            .OrderByDescending(x => x.CompletedAt).ThenByDescending(x => x.Id)
            .Skip((page.Page - 1) * page.PageSize).Take(page.PageSize).ToListAsync(ct);
        return new() { Page = page.Page, PageSize = page.PageSize, Total = total,
            Items = rows.Select(x => MapHistory(x, selectedPlayer)).ToList() };
    }

    public async Task<MatchDetailResponse> GetMatchAsync(long userId, long matchId, CancellationToken ct)
    {
        var user = await CallerAsync(userId, ct);
        if (matchId <= 0) throw Invalid();
        var match = await _context.Matches.AsNoTracking().Include(x => x.MatchPlayers)
            .Include(x => x.MatchRewardResults).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == matchId, ct);
        if (match == null) throw NotFound();
        var ownPlayerId = await _context.Players.Where(x => x.UserId == userId).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct);
        var ownMember = match.MatchPlayers.Any(x => x.PlayerProfileId == ownPlayerId);
        // Participant clients may see placements, but only their own educational answers/rewards.
        var visibleIds = new List<long>();
        if (ownMember) visibleIds.Add(ownPlayerId!.Value);
        else if (user.IsPlatformAdmin) visibleIds.AddRange(match.MatchPlayers.Select(x => x.PlayerProfileId));
        else if (match.CommunityId.HasValue)
        {
            var licenses = await ScopedLicensesAsync(user, match.CommunityId.Value, ct);
            var matchPlayerIds = match.MatchPlayers.Select(x => x.PlayerProfileId).ToList();
            visibleIds = await _access.HasCommunityRole(user.Id, match.CommunityId.Value, new[] { CommunityUserRole.Owner })
                ? matchPlayerIds
                : await licenses.Where(x => matchPlayerIds.Contains(x.PlayerProfileId!.Value))
                    .Select(x => x.PlayerProfileId!.Value).Distinct().ToListAsync(ct);
            if (!match.MatchPlayers.Any(x => visibleIds.Contains(x.PlayerProfileId))) throw NotFound();
        }
        else throw NotFound();
        var players = ownMember ? match.MatchPlayers : match.MatchPlayers.Where(x => visibleIds.Contains(x.PlayerProfileId));
        var questions = await _context.MatchQuestionResults.AsNoTracking().Where(x => x.MatchId == matchId && visibleIds.Contains(x.PlayerProfileId))
            .OrderBy(x => x.PlayerProfileId).ThenBy(x => x.Sequence).ThenBy(x => x.Id).ToListAsync(ct);
        return new() { Match = MapHistory(match, ownMember ? ownPlayerId : null),
            Players = players.OrderBy(x => x.Position).ThenBy(x => x.PlayerProfileId).Select(x => new MatchPlayerResponse
            { PlayerProfileId = x.PlayerProfileId, Position = x.Position, IsWinner = x.IsWinner,
                CorrectAnswers = visibleIds.Contains(x.PlayerProfileId) ? x.CorrectAnswers : null,
                WrongAnswers = visibleIds.Contains(x.PlayerProfileId) ? x.WrongAnswers : null,
                MaxStreak = visibleIds.Contains(x.PlayerProfileId) ? x.MaxStreak : null,
                AnswerTimeTotalMs = visibleIds.Contains(x.PlayerProfileId) ? x.AnswerTimeTotalMs : null,
                QuestionTimeTotalMs = visibleIds.Contains(x.PlayerProfileId) ? x.QuestionTimeTotalMs : null,
                Reward = visibleIds.Contains(x.PlayerProfileId) ? match.MatchRewardResults.Where(r => r.PlayerProfileId == x.PlayerProfileId)
                    .Select(MatchProgressionService.MapReward).SingleOrDefault() : null }).ToList(),
            QuestionResults = questions.Select(x => new MatchAnswerRequest { PlayerProfileId = x.PlayerProfileId,
                QuestionId = x.QuestionId, QuestionType = x.QuestionType, IsCorrect = x.IsCorrect, AnswerTimeMs = x.AnswerTimeMs,
                QuestionTimeMs = x.QuestionTimeMs, StreakBeforeAnswer = x.StreakBeforeAnswer, StreakAfterAnswer = x.StreakAfterAnswer }).ToList() };
    }

    private async Task<User> CallerAsync(long userId, CancellationToken ct)
    {
        var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user == null || user.Status != UserStatus.Active) throw Forbidden();
        return user;
    }

    private async Task<Player> AccessiblePlayerAsync(User user, long? playerId, CancellationToken ct)
    {
        if (playerId is <= 0) throw Invalid();
        var player = await _context.Players.AsNoTracking().SingleOrDefaultAsync(x => playerId.HasValue ? x.Id == playerId : x.UserId == user.Id, ct);
        if (player == null) throw NotFound();
        if (player.UserId == user.Id || user.IsPlatformAdmin) return player;
        var staffCommunity = await _access.ResolveCurrentStaffCommunityId(user.Id, ct);
        if (staffCommunity.HasValue)
        {
            var licenses = await ScopedLicensesAsync(user, staffCommunity.Value, ct);
            if (await licenses.AnyAsync(x => x.PlayerProfileId == player.Id, ct)) return player;
        }
        throw NotFound();
    }

    private async Task<IQueryable<StudentLicense>> ScopedLicensesAsync(User user, long communityId, CancellationToken ct)
    {
        if (communityId <= 0) throw Invalid();
        if (!await _context.Communities.AnyAsync(x => x.Id == communityId && x.Status == CommunityStatus.Active, ct)) throw NotFound();
        var licenses = _context.StudentLicenses.AsNoTracking().Where(x => x.CommunityId == communityId &&
            x.Status == StudentLicenseStatus.Active && x.PlayerProfileId.HasValue && x.Class.Status == ClassStatus.Active &&
            x.Class.CommunityId == communityId && x.Grade.CommunityId == communityId);
        if (user.IsPlatformAdmin) return licenses;
        if (await _access.HasCommunityRole(user.Id, communityId, new[] { CommunityUserRole.Owner })) return licenses;
        if (await _access.HasCommunityRole(user.Id, communityId, new[] { CommunityUserRole.Teacher }))
            return licenses.Where(x => _context.TeacherClassAssignments.Any(a => a.ClassId == x.ClassId && a.TeacherUserId == user.Id));
        throw Forbidden();
    }

    private async Task<IQueryable<StudentLicense>> FilterLicensesAsync(IQueryable<StudentLicense> licenses,
        long communityId, ProgressionPageRequest page, CancellationToken ct)
    {
        if (page.GradeId.HasValue)
        {
            if (!await _context.Grades.AnyAsync(x => x.Id == page.GradeId && x.CommunityId == communityId, ct)) throw NotFound();
            licenses = licenses.Where(x => x.GradeId == page.GradeId);
        }
        if (page.ClassId.HasValue)
        {
            if (!await _context.Classes.AnyAsync(x => x.Id == page.ClassId && x.CommunityId == communityId && x.Status == ClassStatus.Active, ct)) throw NotFound();
            licenses = licenses.Where(x => x.ClassId == page.ClassId);
        }
        return licenses;
    }

    private static MatchHistoryResponse MapHistory(Match x, long? playerId) => new()
    {
        MatchId = x.Id, MatchCode = x.MatchCode, MatchType = x.MatchType.ToString(), Status = x.Status.ToString(),
        CommunityId = x.CommunityId, StartedAt = DateTime.SpecifyKind(x.StartedAt, DateTimeKind.Utc),
        EndedAt = x.EndedAt.HasValue ? DateTime.SpecifyKind(x.EndedAt.Value, DateTimeKind.Utc) : null,
        CompletedAt = x.CompletedAt.HasValue ? DateTime.SpecifyKind(x.CompletedAt.Value, DateTimeKind.Utc) : null,
        TotalPlayers = x.TotalPlayers, Position = x.MatchPlayers.Where(p => p.PlayerProfileId == playerId).Select(p => (int?)p.Position).SingleOrDefault(),
        Reward = x.MatchRewardResults.Where(r => r.PlayerProfileId == playerId).Select(MatchProgressionService.MapReward).SingleOrDefault()
    };

    private static void ValidatePage(ProgressionPageRequest page)
    {
        if (page.Page < 1 || page.PageSize is < 1 or > 100 || (long)(page.Page - 1) * page.PageSize > int.MaxValue ||
            page.GradeId is <= 0 || page.ClassId is <= 0) throw Invalid();
    }
    private static GenericException Invalid() => new(ErrorCode.Failure, ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
    private static GenericException NotFound() => new(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);
    private static GenericException Forbidden() => new(ErrorCode.Failure, ErrorMessage.InvalidAccessToken, HttpStatusCode.Forbidden);
}
