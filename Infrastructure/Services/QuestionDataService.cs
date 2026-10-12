using Domain.Services;
using System.Data;
using System.Net;
using System.Text.Json;
using Domain.Enums;
using Domain.Models.QuestionData;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Exceptions;
using Infrastructure.DataAccess;
using Shared.Requests.QuestionData;
using Shared.Responses;

namespace Infrastructure.Services;

public partial class QuestionDataService(ApplicationDbContext context) : IQuestionDataService
{
    public async Task<Guid> PublishAsync(long adminUserId, QuestionDataRequest request, CancellationToken ct)
    {
        await RequireAdminAsync(adminUserId, ct);
        var question = QuestionDataValidation.Question(request);
        await using var tx = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        try
        {
            var id = await PublishCoreAsync(question, ct);
            if (tx != null) await tx.CommitAsync(ct);
            return id;
        }
        catch (DbUpdateException)
        {
            if (tx != null) await tx.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw Error("Question or answer identifiers conflict with existing records.", HttpStatusCode.Conflict);
        }
        catch
        {
            if (tx != null) await tx.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<ProgressionPage<QuestionDataRequest>> GetBankAsync(QuestionBankFilter filter, CancellationToken ct)
    {
        if (filter == null || filter.Grade <= 0 || filter.Unit <= 0 || filter.Lesson <= 0 || filter.Term <= 0)
            throw Error("A positive grade and positive optional unit, lesson and term are required.");
        Page(filter.PageNumber, filter.PageSize);
        var query = Questions().AsNoTracking().Where(x => x.Grade == filter.Grade);
        if (filter.Unit.HasValue) query = query.Where(x => x.Unit == filter.Unit);
        if (filter.Lesson.HasValue) query = query.Where(x => x.Lesson == filter.Lesson);
        if (filter.Term.HasValue) query = query.Where(x => x.Term == filter.Term);
        if (filter.Curriculum != null) query = query.Where(x => x.Curriculum == filter.Curriculum);
        if (filter.Language != null) query = query.Where(x => x.Language == filter.Language);
        if (filter.Subject != null) query = query.Where(x => x.Subject == filter.Subject);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.QuestionId).Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(ct);
        return new() { Items = rows.Select(QuestionDataMapping.Question).ToList(), Total = total, Page = filter.PageNumber, PageSize = filter.PageSize };
    }

    private IQueryable<Question> Questions() => context.Set<Question>()
        .Include(x => x.Choices).Include(x => x.BooleanAnswers).Include(x => x.AcceptedAnswers)
        .Include(x => x.Items).Include(x => x.Pairs).Include(x => x.Options).Include(x => x.BlankAnswers).AsSplitQuery();

    private async Task RequireAdminAsync(long userId, CancellationToken ct)
    {
        if (!await context.Users.AnyAsync(x => x.Id == userId && x.Status == UserStatus.Active && x.IsPlatformAdmin, ct))
            throw Error("Platform administrator access required.", HttpStatusCode.Forbidden);
    }

    private static void Page(int number, int size)
    {
        if (number is < 1 or > 100000 || size is < 1 or > 200) throw Error("Page must be 1..100000 and page size 1..200.");
    }

    internal static GenericException Error(string message, HttpStatusCode status = HttpStatusCode.BadRequest)
        => new(ErrorCode.Failure, message, status);
}
