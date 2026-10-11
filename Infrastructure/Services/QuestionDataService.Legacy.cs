using Domain.Models;
using Shared.Requests.QuestionData;
using System.Text.Json;
using System.Data;
using System.Net;
using Domain.Models.QuestionData;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace Infrastructure.Services;

public partial class QuestionDataService
{
    public async Task<List<QuestionDataRequest>> ImportLegacyAsync(long adminUserId, long packId, CancellationToken ct)
    {
        await RequireAdminAsync(adminUserId, ct);
        await using var tx = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        try
        {
            var pack = await context.Questions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == packId, ct)
                ?? throw Error("Legacy pack not found.", HttpStatusCode.NotFound);
            var requests = LegacyQuestionConverter.Convert(pack);
            var converted = requests.Select(QuestionDataValidation.Question).ToList();
            var ids = converted.Select(x => x.QuestionId).ToList();
            var existing = await Questions().AsNoTracking().Where(x => ids.Contains(x.QuestionId)).ToDictionaryAsync(x => x.QuestionId, ct);
            foreach (var question in converted)
            {
                if (existing.TryGetValue(question.QuestionId, out var published))
                {
                    if (!SameContent(published, question)) throw Error("Published question content is immutable; supply a new question ID.", HttpStatusCode.Conflict);
                }
                else context.Set<Question>().Add(question);
            }
            await context.SaveChangesAsync(ct);
            if (tx != null) await tx.CommitAsync(ct);
            return requests;
        }
        catch (DbUpdateException)
        {
            if (tx != null) await tx.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw Error("Legacy question or answer IDs conflict with existing published data.", HttpStatusCode.Conflict);
        }
        catch
        {
            if (tx != null) await tx.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear(); throw;
        }
    }

    public async Task<JsonElement?> ProjectLegacyAsync(QuestionsJson pack, CancellationToken ct)
    {
        List<QuestionDataRequest> expected;
        try { expected = LegacyQuestionConverter.Convert(pack); }
        catch (GenericException) { return null; }
        var ids = expected.Select(x => x.QuestionId).ToList();
        var stored = await Questions().AsNoTracking().Where(x => ids.Contains(x.QuestionId)).ToDictionaryAsync(x => x.QuestionId, ct);
        if (stored.Count != expected.Count) return null;
        var results = new List<QuestionDataRequest>();
        foreach (var request in expected)
        {
            var actual = QuestionDataMapping.Question(stored[request.QuestionId]);
            // A legacy UUID may only project matching published content, not substitute an unrelated question.
            if (!SameContent(stored[request.QuestionId], QuestionDataValidation.Question(request))) return null;
            results.Add(actual);
        }
        return LegacyQuestionConverter.Project(pack, results);
    }

    private async Task<Guid> PublishCoreAsync(Question question, CancellationToken ct)
    {
        var existing = await Questions().AsNoTracking().SingleOrDefaultAsync(x => x.QuestionId == question.QuestionId, ct);
        if (existing != null)
        {
            if (!SameContent(existing, question))
                throw Error("Published question content is immutable; supply a new question ID.", HttpStatusCode.Conflict);
            return existing.QuestionId;
        }
        context.Set<Question>().Add(question);
        await context.SaveChangesAsync(ct);
        return question.QuestionId;
    }

    private static bool SameContent(Question first, Question second)
        => JsonSerializer.Serialize(QuestionDataMapping.Question(first)) == JsonSerializer.Serialize(QuestionDataMapping.Question(second));

    public async Task<JsonElement?> ProjectBankAsync(QuestionBankFilter filter, CancellationToken ct)
    {
        // Keep every page/child query on one snapshot while educational publishing can add UUIDs.
        await using var tx = context.Database.IsRelational() ? await context.Database.BeginTransactionAsync(
            context.Database.IsMySql() ? IsolationLevel.RepeatableRead : IsolationLevel.Serializable, ct) : null;
        var all = new List<QuestionDataRequest>();
        var pageFilter = new QuestionBankFilter { Grade = filter.Grade, Unit = filter.Unit, Lesson = filter.Lesson,
            Curriculum = filter.Curriculum, Language = filter.Language, Subject = filter.Subject, Term = filter.Term,
            PageNumber = 1, PageSize = 200 };
        while (true)
        {
            var page = await GetBankAsync(pageFilter, ct);
            if (page.Total > 2000) throw Error("Compatible packs are limited to 2000 questions; use the paged bank API.");
            all.AddRange(page.Items);
            if (all.Count >= page.Total || page.Items.Count == 0) break;
            pageFilter.PageNumber++;
        }
        if (tx != null) await tx.CommitAsync(ct);
        return all.Count == 0 ? null : LegacyQuestionConverter.ProjectBank(all);
    }
}
