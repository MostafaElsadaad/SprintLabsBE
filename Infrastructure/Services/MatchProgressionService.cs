using System.Data;
using System.Data.Common;
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
using MatchType = Domain.Enums.MatchType;

namespace Infrastructure.Services;

public class MatchProgressionService : IMatchProgressionService
{
    private readonly ApplicationDbContext _context;
    private readonly IXpCalculationService _xp;
    private readonly ILevelProgressionService _levels;
    private readonly IRpRankCalculationService _ranks;

    public MatchProgressionService(ApplicationDbContext context, IXpCalculationService xp,
        ILevelProgressionService levels, IRpRankCalculationService ranks)
    {
        _context = context;
        _xp = xp;
        _levels = levels;
        _ranks = ranks;
    }

    public async Task<MatchRegistrationResponse> RegisterAsync(RegisterMatchRequest request, CancellationToken ct)
    {
        try { return await RegisterCoreAsync(request, ct); }
        catch (DbUpdateException) { throw PersistenceUnavailable(); }
        catch (DbException) { throw PersistenceUnavailable(); }
    }

    private async Task<MatchRegistrationResponse> RegisterCoreAsync(RegisterMatchRequest request, CancellationToken ct)
    {
        var type = ParseMatchType(request.MatchType);
        if (string.IsNullOrWhiteSpace(request.MatchCode) || request.MatchCode.Length > 64 ||
            request.MirrorRoomId?.Length > 128 || request.CommunityId is <= 0 ||
            request.StartedAt.Kind != DateTimeKind.Utc || request.StartedAt.Year < 1000 || request.StartedAt > DateTime.UtcNow.AddMinutes(5) ||
            request.PlayerProfileIds == null || request.PlayerProfileIds.Count is < 1 or > 128 ||
            request.PlayerProfileIds.Any(x => x <= 0) || request.PlayerProfileIds.Distinct().Count() != request.PlayerProfileIds.Count ||
            request.TotalPlayers < request.PlayerProfileIds.Count || request.TotalPlayers > 128 ||
            (type == MatchType.Ranked && request.TotalPlayers < 2)) throw Invalid();

        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        var existing = await _context.Matches.Include(x => x.MatchPlayers)
            .SingleOrDefaultAsync(x => x.MatchCode == request.MatchCode, ct);
        if (existing != null)
        {
            if (existing.MatchType != type || existing.CommunityId != request.CommunityId ||
                existing.MirrorRoomId != request.MirrorRoomId || existing.StartedAt != DatabaseTimestamp(request.StartedAt) ||
                existing.TotalPlayers != request.TotalPlayers ||
                !existing.MatchPlayers.Select(x => x.PlayerProfileId).Order().SequenceEqual(request.PlayerProfileIds.Order()))
                throw Conflict();
            return new() { MatchId = existing.Id, Status = existing.Status.ToString() };
        }

        await EnsureEligiblePlayersAsync(request.PlayerProfileIds, request.CommunityId, ct);
        var match = new Match
        {
            MatchCode = request.MatchCode, MirrorRoomId = request.MirrorRoomId, MatchType = type,
            CommunityId = request.CommunityId, StartedAt = DatabaseTimestamp(request.StartedAt),
            TotalPlayers = request.TotalPlayers, Status = MatchStatus.Started,
            MatchPlayers = request.PlayerProfileIds.Select(id => new MatchPlayer
            { PlayerProfileId = id, CommunityId = request.CommunityId }).ToList()
        };
        _context.Matches.Add(match);
        await _context.SaveChangesAsync(ct);
        if (transaction != null) await transaction.CommitAsync(ct);
        return new() { MatchId = match.Id, Status = match.Status.ToString() };
    }

    public async Task<MatchCompletionResponse> CompleteAsync(long matchId, CompleteMatchRequest request, CancellationToken ct)
    {
        try { return await CompleteCoreAsync(matchId, request, ct); }
        catch (DbUpdateException) { throw PersistenceUnavailable(); }
        catch (DbException) { throw PersistenceUnavailable(); }
    }

    private async Task<MatchCompletionResponse> CompleteCoreAsync(long matchId, CompleteMatchRequest request, CancellationToken ct)
    {
        if (matchId <= 0) throw Invalid();
        await using var transaction = _context.Database.IsRelational()
            ? await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;

        // Lock the match before reading completion state. All API processes share this database lock.
        var match = _context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true
            ? (await _context.Matches.FromSqlInterpolated($"SELECT * FROM Matches WHERE Id = {matchId} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()
            : await _context.Matches.SingleOrDefaultAsync(x => x.Id == matchId, ct);
        if (match == null) throw NotFound();
        if (match.Status == MatchStatus.Completed)
        {
            var saved = await _context.MatchRewardResults.AsNoTracking().Where(x => x.MatchId == matchId)
                .OrderBy(x => x.PlayerProfileId).ToListAsync(ct);
            return Result(matchId, saved);
        }
        if (match.Status != MatchStatus.Started) throw Conflict();
        await _context.Entry(match).Collection(x => x.MatchPlayers).LoadAsync(ct);
        ValidateCompletion(match, request);

        // Lock profiles in a stable order: different matches may reward the same human concurrently.
        var profiles = new Dictionary<long, Player>();
        foreach (var id in request.Players.Select(x => x.PlayerProfileId).Order())
        {
            var player = _context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true
                ? (await _context.Players.FromSqlInterpolated($"SELECT * FROM Players WHERE Id = {id} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()
                : await _context.Players.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (player == null) throw NotFound();
            profiles.Add(id, player);
        }
        // Membership was checked at registration; finishing a started match preserves that historical context.
        var now = DateTime.UtcNow;
        var rewards = new List<MatchRewardResult>();
        foreach (var facts in request.Players.OrderBy(x => x.PlayerProfileId))
        {
            var player = profiles[facts.PlayerProfileId];
            if (player.Experience < 0 || player.Rp < 0 || player.TotalMatches == int.MaxValue ||
                player.TotalWins == int.MaxValue) throw Conflict();
            var answers = request.QuestionResults.Where(x => x.PlayerProfileId == player.Id).ToList();
            var xp = _xp.CalculateXp(answers.Select(x => new XpQuestionResult { IsCorrect = x.IsCorrect }), facts.Position == 1);
            if (player.Experience > int.MaxValue - xp.TotalXp) throw Conflict();
            var level = _levels.CalculateProgression(new LevelProgressionRequest { OldTotalXp = player.Experience, XpGained = xp.TotalXp });
            var rank = _ranks.CalculateRpAndRank(player.Rp, match.MatchType, facts.Position, match.TotalPlayers);
            var oldTier = match.MatchType == MatchType.Ranked ? (RankTier)rank.OldRankTier : player.RankTier;
            var newTier = match.MatchType == MatchType.Ranked ? (RankTier)rank.NewRankTier : player.RankTier;
            var reward = new MatchRewardResult
            {
                MatchId = matchId, PlayerProfileId = player.Id, CommunityId = match.CommunityId,
                AnswerXp = xp.AnswerXp, MatchResultXp = xp.MatchResultXp, MissionXp = 0, TotalXp = xp.TotalXp,
                OldLevel = level.OldLevel, NewLevel = level.NewLevel, OldTotalXp = level.OldTotalXp, NewTotalXp = level.NewTotalXp,
                OldRp = rank.OldRp, NewRp = rank.NewRp, RpChange = rank.RpChange,
                OldRankTier = oldTier, NewRankTier = newTier, CreatedAt = now
            };
            rewards.Add(reward);
            player.Experience = level.NewTotalXp;
            player.Level = level.NewLevel;
            player.Rp = rank.NewRp;
            player.RankTier = newTier;
            if (newTier > player.HighestRankTier) player.HighestRankTier = newTier;
            player.TotalMatches++;
            if (facts.Position == 1) player.TotalWins++;
            player.UpdatedAt = now;

            var member = match.MatchPlayers.Single(x => x.PlayerProfileId == player.Id);
            member.Position = facts.Position; member.IsWinner = facts.Position == 1;
            member.CorrectAnswers = facts.CorrectAnswers; member.WrongAnswers = facts.WrongAnswers;
            member.MaxStreak = facts.MaxStreak; member.AnswerTimeTotalMs = facts.AnswerTimeTotalMs;
            member.QuestionTimeTotalMs = facts.QuestionTimeTotalMs;

            var sequence = 0;
            foreach (var answer in answers)
            {
                var question = new MatchQuestionResult
                {
                    MatchId = matchId, PlayerProfileId = player.Id, QuestionId = answer.QuestionId, Sequence = ++sequence,
                    QuestionType = answer.QuestionType, IsCorrect = answer.IsCorrect,
                    AnswerTimeMs = answer.AnswerTimeMs, QuestionTimeMs = answer.QuestionTimeMs,
                    StreakBeforeAnswer = answer.StreakBeforeAnswer, StreakAfterAnswer = answer.StreakAfterAnswer, CreatedAt = now
                };
                _context.MatchQuestionResults.Add(question);
                if (answer.IsCorrect)
                {
                    var amount = _xp.CalculateCorrectAnswerXp(answer.StreakAfterAnswer);
                    // SourceId identifies the match; question-level timing/streak facts are retained separately.
                    var log = new PlayerXpLog { PlayerProfileId = player.Id, CommunityId = match.CommunityId,
                        SourceType = XpSourceType.MatchAnswer, SourceId = matchId, BaseXp = 10,
                        Multiplier = amount / 10m, FinalXp = amount, CreatedAt = now };
                    _context.PlayerXpLogs.Add(log);
                }
            }
            _context.PlayerXpLogs.Add(new PlayerXpLog { PlayerProfileId = player.Id, CommunityId = match.CommunityId,
                SourceType = XpSourceType.MatchResult, SourceId = matchId, BaseXp = xp.MatchResultXp,
                Multiplier = 1, FinalXp = xp.MatchResultXp, CreatedAt = now });
            if (match.MatchType == MatchType.Ranked)
                _context.PlayerRankLogs.Add(new PlayerRankLog { MatchId = matchId, PlayerProfileId = player.Id,
                    CommunityId = match.CommunityId, OldRp = rank.OldRp, RpChange = rank.RpChange, NewRp = rank.NewRp,
                    OldRankTier = oldTier, NewRankTier = newTier, Position = facts.Position,
                    TotalPlayers = match.TotalPlayers, CreatedAt = now });
        }
        _context.MatchRewardResults.AddRange(rewards);
        match.Status = MatchStatus.Completed;
        match.EndedAt = request.EndedAt;
        match.CompletedAt = now;
        await _context.SaveChangesAsync(ct);
        if (transaction != null) await transaction.CommitAsync(ct);
        return Result(matchId, rewards);
    }

    private async Task EnsureEligiblePlayersAsync(List<long> ids, long? communityId, CancellationToken ct)
    {
        var eligible = await _context.Players.Where(x => ids.Contains(x.Id) && x.UserId.HasValue &&
            _context.Users.Any(u => u.Id == x.UserId && u.Status == UserStatus.Active)).Select(x => x.Id).ToListAsync(ct);
        if (eligible.Count != ids.Count) throw NotFound();
        if (communityId.HasValue)
        {
            if (!await _context.Communities.AnyAsync(x => x.Id == communityId && x.Status == CommunityStatus.Active, ct)) throw NotFound();
            var licensed = await _context.StudentLicenses.Where(x => x.CommunityId == communityId &&
                x.Status == StudentLicenseStatus.Active && x.PlayerProfileId.HasValue && ids.Contains(x.PlayerProfileId.Value) &&
                x.Class.Status == ClassStatus.Active && x.Class.CommunityId == communityId && x.Grade.CommunityId == communityId &&
                _context.Players.Any(p => p.Id == x.PlayerProfileId && p.UserId == x.UserId))
                .Select(x => x.PlayerProfileId!.Value).Distinct().CountAsync(ct);
            if (licensed != ids.Count) throw Invalid();
        }
    }

    private static void ValidateCompletion(Match match, CompleteMatchRequest request)
    {
        if (ParseMatchType(request.MatchType) != match.MatchType || request.CommunityId != match.CommunityId ||
            request.MirrorRoomId != match.MirrorRoomId || request.EndedAt.Kind != DateTimeKind.Utc ||
            request.EndedAt < match.StartedAt || request.EndedAt > DateTime.UtcNow.AddMinutes(5) ||
            request.Players == null || request.QuestionResults == null || request.Players.Count is < 1 or > 128 ||
            request.QuestionResults.Count > 12800 || request.Players.Any(x => x == null) ||
            request.QuestionResults.Any(x => x == null)) throw Invalid();
        var ids = request.Players.Select(x => x.PlayerProfileId).ToList();
        if (ids.Distinct().Count() != ids.Count || !ids.Order().SequenceEqual(match.MatchPlayers.Select(x => x.PlayerProfileId).Order()) ||
            request.Players.Any(x => x.Position < 1 || x.Position > match.TotalPlayers) ||
            request.Players.Select(x => x.Position).Distinct().Count() != ids.Count ||
            request.QuestionResults.Any(x => !ids.Contains(x.PlayerProfileId))) throw Invalid();
        foreach (var player in request.Players)
        {
            var answers = request.QuestionResults.Where(x => x.PlayerProfileId == player.PlayerProfileId).ToList();
            if (answers.Count > 1000) throw Invalid();
            var streak = 0; var max = 0;
            long answerTime = 0; long questionTime = 0;
            foreach (var answer in answers)
            {
                if (string.IsNullOrWhiteSpace(answer.QuestionType) || answer.QuestionType.Length > 64 ||
                    answer.QuestionId is <= 0 || answer.AnswerTimeMs < 0 || answer.QuestionTimeMs <= 0 ||
                    answer.AnswerTimeMs > answer.QuestionTimeMs || answer.StreakBeforeAnswer != streak) throw Invalid();
                streak = answer.IsCorrect ? streak + 1 : 0;
                if (answer.StreakAfterAnswer != streak) throw Invalid();
                max = Math.Max(max, streak);
                answerTime += answer.AnswerTimeMs; questionTime += answer.QuestionTimeMs;
            }
            if (player.CorrectAnswers != answers.Count(x => x.IsCorrect) || player.WrongAnswers != answers.Count(x => !x.IsCorrect) ||
                player.MaxStreak != max || player.AnswerTimeTotalMs != answerTime || player.QuestionTimeTotalMs != questionTime) throw Invalid();
        }
    }

    private static MatchType ParseMatchType(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || int.TryParse(value, out _) ||
            !Enum.TryParse<MatchType>(value, true, out var type) || !Enum.IsDefined(type)) throw Invalid();
        return type;
    }

    private static MatchCompletionResponse Result(long id, IEnumerable<MatchRewardResult> rewards) => new()
    { MatchId = id, Status = MatchStatus.Completed.ToString(), Rewards = rewards.Select(MapReward).ToList() };

    internal static MatchRewardResponse MapReward(MatchRewardResult x) => new()
    {
        PlayerProfileId = x.PlayerProfileId, AnswerXp = x.AnswerXp, MatchResultXp = x.MatchResultXp,
        MissionXp = x.MissionXp, XpGained = x.TotalXp, OldLevel = x.OldLevel, NewLevel = x.NewLevel,
        OldTotalXp = x.OldTotalXp, NewTotalXp = x.NewTotalXp, RpChange = x.RpChange, OldRp = x.OldRp,
        NewRp = x.NewRp, OldRankTier = x.OldRankTier.ToString(), NewRankTier = x.NewRankTier.ToString()
    };

    private static GenericException Invalid() => new(ErrorCode.Failure, ErrorMessage.InvalidInput, HttpStatusCode.BadRequest);
    private static GenericException NotFound() => new(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);
    private static GenericException Conflict() => new(ErrorCode.Failure, ErrorMessage.ExistingRecord, HttpStatusCode.Conflict);
    private static GenericException PersistenceUnavailable() => new(ErrorCode.Failure, ErrorMessage.GeneralError, HttpStatusCode.ServiceUnavailable);
    // MySQL datetime(6) cannot round-trip .NET's seventh fractional digit.
    private static DateTime DatabaseTimestamp(DateTime value) => new(value.Ticks - value.Ticks % 10, DateTimeKind.Utc);
}
