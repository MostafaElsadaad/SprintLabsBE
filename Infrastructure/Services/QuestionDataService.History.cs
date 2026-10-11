using System.Data;
using System.Net;
using System.Text.Json;
using Domain.Enums;
using Domain.Models.QuestionData;
using Microsoft.EntityFrameworkCore;
using Shared.Requests.QuestionData;
using Shared.Responses;

namespace Infrastructure.Services;

public partial class QuestionDataService
{
    public async Task<QuestionHistoryResponse> RecordAsync(QuestionHistoryRequest request, CancellationToken ct, long? historyId = null)
    {
        if (request == null || request.QuestionId == Guid.Empty || request.PlayerId <= 0 || request.MatchId <= 0 || request.TimeTakenSeconds < 0 || historyId <= 0)
            throw Error("Question, player, match and nonnegative elapsed time are required.");
        QuestionDataValidation.Seconds(request.TimeTakenSeconds, allowZero: true);
        await using var tx = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        try
        {
            if (!await context.MatchPlayers.AnyAsync(x => x.MatchId == request.MatchId && x.PlayerProfileId == request.PlayerId, ct))
                throw Error("The player is not a registered human member of this match.");
            var question = await Questions().AsNoTracking().SingleOrDefaultAsync(x => x.QuestionId == request.QuestionId, ct)
                ?? throw Error("Question not found.", HttpStatusCode.NotFound);
            var history = new QuestionHistory { PlayerId = request.PlayerId, QuestionId = request.QuestionId,
                MatchId = request.MatchId, TimeTakenSeconds = request.TimeTakenSeconds };
            AddResponse(question, request, history);
            if (historyId.HasValue)
            {
                var stored = await HistoryRows().SingleOrDefaultAsync(x => x.HistoryId == historyId, ct)
                    ?? throw Error("History attempt not found.", HttpStatusCode.NotFound);
                if (stored.QuestionId != request.QuestionId) throw Error("An existing attempt cannot be changed.", HttpStatusCode.Conflict);
                history.HistoryId = stored.HistoryId;
                var original = QuestionDataMapping.History(stored);
                var repeated = QuestionDataMapping.History(history);
                original.IsCorrect = Correct(question, original);
                repeated.IsCorrect = Correct(question, repeated);
                if (JsonSerializer.Serialize(original) != JsonSerializer.Serialize(repeated))
                    throw Error("An existing attempt cannot be changed.", HttpStatusCode.Conflict);
                if (tx != null) await tx.CommitAsync(ct);
                return original;
            }
            context.Set<QuestionHistory>().Add(history);
            await context.SaveChangesAsync(ct);
            if (tx != null) await tx.CommitAsync(ct);
            var result = QuestionDataMapping.History(history);
            result.IsCorrect = Correct(question, result);
            return result;
        }
        catch (DbUpdateException)
        {
            if (tx != null) await tx.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw Error("Question history references conflict with existing records.", HttpStatusCode.Conflict);
        }
        catch
        {
            if (tx != null) await tx.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<ProgressionPage<QuestionHistoryResponse>> HistoryAsync(long? userId, long playerId, long? matchId,
        int pageNumber, int pageSize, CancellationToken ct)
    {
        Page(pageNumber, pageSize);
        if (playerId <= 0 || matchId <= 0) throw Error("Positive player and optional match IDs are required.");
        if (userId.HasValue)
        {
            var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId && x.Status == UserStatus.Active, ct)
                ?? throw Error("Active account required.", HttpStatusCode.Forbidden);
            if (!user.IsPlatformAdmin && !await context.Players.AnyAsync(x => x.Id == playerId && x.UserId == user.Id, ct))
                throw Error("Only your own question history is available.", HttpStatusCode.Forbidden);
        }
        var query = HistoryRows().Where(x => x.PlayerId == playerId);
        if (matchId.HasValue) query = query.Where(x => x.MatchId == matchId);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.HistoryId).Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        var ids = rows.Select(x => x.QuestionId).Distinct().ToList();
        var questions = await Questions().AsNoTracking().Where(x => ids.Contains(x.QuestionId)).ToDictionaryAsync(x => x.QuestionId, ct);
        var responses = rows.Select(QuestionDataMapping.History).ToList();
        foreach (var response in responses) response.IsCorrect = Correct(questions[response.QuestionId], response);
        return new() { Items = responses, Total = total, Page = pageNumber, PageSize = pageSize };
    }

    private IQueryable<QuestionHistory> HistoryRows() => context.Set<QuestionHistory>().AsNoTracking()
        .Include(x => x.MCQHistoryRows).Include(x => x.TrueFalseHistoryRows).Include(x => x.FillBlankHistoryRows)
        .Include(x => x.OrderingHistoryRows).Include(x => x.MatchingHistoryRows).Include(x => x.DragDropHistoryRows).AsSplitQuery();

    private static void AddResponse(Question q, QuestionHistoryRequest r, QuestionHistory h)
    {
        if (r.Ordering == null || r.Matching == null || r.DragDrop == null) throw Error("Response collections cannot be null.");
        var groups = new[] { r.SelectedChoiceId != null, r.SelectedAnswer.HasValue, r.AnswerText != null,
            r.Ordering.Count > 0, r.Matching.Count > 0, r.DragDrop.Count > 0 };
        if (groups.Count(x => x) != 1 || !groups[(int)q.QuestionType]) throw Error("Supply exactly the response for this question type.");
        switch (q.QuestionType)
        {
            case QuestionType.MCQ:
                if (!q.Choices.Any(x => x.ChoiceId == r.SelectedChoiceId)) throw Error("Selected choice does not belong to this question.");
                h.MCQHistoryRows.Add(new() { SelectedChoiceId = r.SelectedChoiceId! });
                break;
            case QuestionType.TrueOrFalse:
                h.TrueFalseHistoryRows.Add(new() { SelectedAnswer = r.SelectedAnswer!.Value });
                break;
            case QuestionType.FillBlank:
                if (r.AnswerText!.Length > 4000) throw Error("Typed answers are limited to 4000 characters.");
                h.FillBlankHistoryRows.Add(new() { AnswerText = r.AnswerText });
                break;
            case QuestionType.Ordering:
                if (r.Ordering.Any(x => x == null) || r.Ordering.Count != q.Items.Count) throw Error("Supply one placement for every item.");
                QuestionDataValidation.Ids(r.Ordering.Select(x => x.ItemId));
                QuestionDataValidation.Positions(r.Ordering.Select(x => x.SelectedPosition), q.Items.Count);
                var itemIds = q.Items.Select(x => x.ItemId).ToHashSet(StringComparer.Ordinal);
                if (r.Ordering.Any(x => !itemIds.Contains(x.ItemId))) throw Error("An ordering item belongs to another question.");
                foreach (var x in r.Ordering) h.OrderingHistoryRows.Add(new() { ItemId = x.ItemId, SelectedPosition = x.SelectedPosition });
                break;
            case QuestionType.MatchingPairs:
                if (r.Matching.Any(x => x == null) || r.Matching.Count != q.Pairs.Count) throw Error("Supply a match for every left item.");
                QuestionDataValidation.Ids(r.Matching.Select(x => x.LeftPairId)); QuestionDataValidation.Ids(r.Matching.Select(x => x.SelectedRightPairId));
                var pairIds = q.Pairs.Select(x => x.PairId).ToHashSet(StringComparer.Ordinal);
                if (r.Matching.Any(x => !pairIds.Contains(x.LeftPairId) || !pairIds.Contains(x.SelectedRightPairId))) throw Error("A matching item belongs to another question.");
                foreach (var x in r.Matching) h.MatchingHistoryRows.Add(new() { LeftPairId = x.LeftPairId, SelectedRightPairId = x.SelectedRightPairId });
                break;
            case QuestionType.DragAndDrop:
                if (r.DragDrop.Any(x => x == null) || r.DragDrop.Count != q.BlankAnswers.Count) throw Error("Supply a choice for every blank.");
                QuestionDataValidation.Positions(r.DragDrop.Select(x => x.BlankNumber), q.BlankAnswers.Count);
                QuestionDataValidation.Ids(r.DragDrop.Select(x => x.SelectedOptionId));
                var optionIds = q.Options.Select(x => x.OptionId).ToHashSet(StringComparer.Ordinal);
                if (r.DragDrop.Any(x => !optionIds.Contains(x.SelectedOptionId))) throw Error("A selected option belongs to another question.");
                foreach (var x in r.DragDrop) h.DragDropHistoryRows.Add(new() { BlankNumber = x.BlankNumber, SelectedOptionId = x.SelectedOptionId });
                break;
        }
    }

    private static bool Correct(Question q, QuestionHistoryRequest r) => q.QuestionType switch
    {
        QuestionType.MCQ => q.Choices.Single(x => x.ChoiceId == r.SelectedChoiceId).IsCorrect,
        QuestionType.TrueOrFalse => q.BooleanAnswers.Single().CorrectAnswer == r.SelectedAnswer,
        QuestionType.FillBlank => q.AcceptedAnswers.Any(x => QuestionDataValidation.Normalize(x.AcceptedAnswer) == QuestionDataValidation.Normalize(r.AnswerText)),
        QuestionType.Ordering => r.Ordering.All(x => q.Items.Single(i => i.ItemId == x.ItemId).CorrectPosition == x.SelectedPosition),
        QuestionType.MatchingPairs => r.Matching.All(x => x.LeftPairId == x.SelectedRightPairId),
        QuestionType.DragAndDrop => r.DragDrop.All(x => q.Options.Single(o => o.OptionId == x.SelectedOptionId).OptionText == q.BlankAnswers.Single(b => b.BlankNumber == x.BlankNumber).CorrectText),
        _ => false
    };
}
