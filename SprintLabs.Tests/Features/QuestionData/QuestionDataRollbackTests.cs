using Domain.Models.QuestionData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Exceptions;

namespace Compass.Tests.Features.QuestionData;

public class QuestionDataRollbackTests
{
    [Fact]
    public async Task Failure_after_sql_writes_rolls_back_parent_and_response_and_allows_clean_retry()
    {
        var interceptor = new FailAfterHistorySave();
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync(interceptor);
        var q = QuestionDataTypesTests.Question("TrueOrFalse");
        await f.Service.PublishAsync(40, q, default);
        interceptor.Armed = true;
        await Assert.ThrowsAsync<GenericException>(() => f.Service.RecordAsync(QuestionDataTypesTests.Response(q), default));
        Assert.Equal(0, await f.Context.Set<QuestionHistory>().CountAsync());
        Assert.Equal(0, await f.Context.Set<TrueFalseHistory>().CountAsync());
        interceptor.Armed = false;
        Assert.True((await f.Service.RecordAsync(QuestionDataTypesTests.Response(q), default)).IsCorrect);
        Assert.Equal(1, await f.Context.Set<QuestionHistory>().CountAsync());
    }

    private sealed class FailAfterHistorySave : SaveChangesInterceptor
    {
        public bool Armed { get; set; }
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (Armed && eventData.Context!.ChangeTracker.Entries<QuestionHistory>().Any())
                throw new DbUpdateException("Injected post-SQL failure.");
            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }
    }
}
