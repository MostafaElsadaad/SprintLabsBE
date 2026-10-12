using System.Net;
using Domain.Enums;
using Domain.Models;
using Domain.Models.QuestionData;
using Infrastructure.DataAccess;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Shared.Requests.QuestionData;

namespace Compass.Tests.Features.QuestionData;

public class QuestionDataServiceTests
{
    private static QuestionDataRequest Mcq() => new()
    {
        QuestionId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"), Curriculum = "Egypt",
        Grade = 7, Language = "en", Subject = "Science", Term = 1, Unit = 1, Lesson = 1,
        QuestionType = "MCQ", QuestionText = "The building and structural unit of all matter is the @.",
        Choices = new() { new() { ChoiceId = "q001-c0", ChoiceText = "molecule", IsCorrect = false },
            new() { ChoiceId = "q001-c1", ChoiceText = "atom", IsCorrect = true } }
    };

    [Fact]
    public async Task Publish_preserves_uuid_and_choices_and_identical_retry_does_not_duplicate()
    {
        await using var f = await Fixture.CreateAsync();
        var request = Mcq();
        Assert.Equal(request.QuestionId, await f.Service.PublishAsync(40, request, default));
        Assert.Equal(request.QuestionId, await f.Service.PublishAsync(40, Mcq(), default));
        f.Context.ChangeTracker.Clear();
        Assert.Equal(1, await f.Context.Set<Question>().CountAsync());
        Assert.Equal(2, await f.Context.Set<MCQChoice>().CountAsync());
        var bank = await f.Service.GetBankAsync(new() { Grade = 7, Unit = 1 }, default);
        Assert.Equal("atom", Assert.Single(bank.Items).Choices.Single(x => x.IsCorrect == true).ChoiceText);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Publish_preserves_positive_term_metadata(int term)
    {
        await using var f = await Fixture.CreateAsync();
        var request = Mcq();
        request.Term = term;
        await f.Service.PublishAsync(40, request, default);
        f.Context.ChangeTracker.Clear();
        var stored = Assert.Single((await f.Service.GetBankAsync(new() { Grade = 7 }, default)).Items);
        Assert.Equal(term, stored.Term);
        Assert.Equal(request.QuestionId, stored.QuestionId);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task Bank_positive_term_filter_returns_only_matching_questions(int term)
    {
        await using var f = await Fixture.CreateAsync();
        var matching = QuestionDataTypesTests.Question("TrueOrFalse");
        matching.Term = term;
        var other = Mcq();
        other.Term = term == 1 ? 2 : 1;
        await f.Service.PublishAsync(40, matching, default);
        await f.Service.PublishAsync(40, other, default);
        f.Context.ChangeTracker.Clear();
        var bank = await f.Service.GetBankAsync(new() { Grade = 7, Term = term }, default);
        Assert.Equal(1, bank.Total);
        var stored = Assert.Single(bank.Items);
        Assert.Equal(matching.QuestionId, stored.QuestionId);
        Assert.Equal(term, stored.Term);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Database_rejects_nonpositive_term_without_changing_stored_metadata(int term)
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service.PublishAsync(40, Mcq(), default);
        var stored = await f.Context.Set<Question>().SingleAsync();
        stored.Term = term;
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Context.SaveChangesAsync());
        f.Context.ChangeTracker.Clear();
        Assert.Equal(1, (await f.Context.Set<Question>().SingleAsync()).Term);
    }

    [Theory]
    [InlineData(7, 0, 1, 1)]
    [InlineData(7, -1, 1, 1)]
    [InlineData(0, 1, 1, 1)]
    [InlineData(-1, 1, 1, 1)]
    [InlineData(7, 1, 0, 1)]
    [InlineData(7, 1, -1, 1)]
    [InlineData(7, 1, 1, 0)]
    [InlineData(7, 1, 1, -1)]
    public async Task Invalid_bank_metadata_filter_is_rejected(int grade, int term, int unit, int lesson)
    {
        await using var f = await Fixture.CreateAsync();
        var error = await Assert.ThrowsAsync<GenericException>(() => f.Service.GetBankAsync(
            new() { Grade = grade, Term = term, Unit = unit, Lesson = lesson }, default));
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
    }

    [Theory]
    [InlineData(7, 0, 1, 1)]
    [InlineData(7, -1, 1, 1)]
    [InlineData(0, 1, 1, 1)]
    [InlineData(-1, 1, 1, 1)]
    [InlineData(7, 1, 0, 1)]
    [InlineData(7, 1, -1, 1)]
    [InlineData(7, 1, 1, 0)]
    [InlineData(7, 1, 1, -1)]
    public async Task Invalid_metadata_is_rejected_without_partial_rows(int grade, int term, int unit, int lesson)
    {
        await using var f = await Fixture.CreateAsync();
        var request = Mcq();
        request.Grade = grade;
        request.Term = term;
        request.Unit = unit;
        request.Lesson = lesson;
        var error = await Assert.ThrowsAsync<GenericException>(() => f.Service.PublishAsync(40, request, default));
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Equal(0, await f.Context.Set<Question>().CountAsync());
        Assert.Equal(0, await f.Context.Set<MCQChoice>().CountAsync());
    }

    [Fact]
    public async Task Conflicting_publish_and_non_admin_publish_are_rejected_without_changing_content()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service.PublishAsync(40, Mcq(), default);
        var changed = Mcq(); changed.Choices[1].ChoiceText = "cell";
        var conflict = await Assert.ThrowsAsync<GenericException>(() => f.Service.PublishAsync(40, changed, default));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        var denied = await Assert.ThrowsAsync<GenericException>(() => f.Service.PublishAsync(1, changed, default));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        f.Context.ChangeTracker.Clear();
        Assert.Equal("atom", (await f.Context.Set<MCQChoice>().SingleAsync(x => x.ChoiceId == "q001-c1")).ChoiceText);
    }

    [Theory]
    [InlineData("missing-id")]
    [InlineData("two-correct")]
    [InlineData("wrong-type-data")]
    public async Task Invalid_question_is_rejected_without_partial_rows(string invalid)
    {
        await using var f = await Fixture.CreateAsync();
        var request = Mcq();
        if (invalid == "missing-id") request.QuestionId = Guid.Empty;
        if (invalid == "two-correct") request.Choices[0].IsCorrect = true;
        if (invalid == "wrong-type-data") request.CorrectAnswer = false;
        await Assert.ThrowsAsync<GenericException>(() => f.Service.PublishAsync(40, request, default));
        Assert.Equal(0, await f.Context.Set<Question>().CountAsync());
    }

    [Fact]
    public async Task Record_retains_choice_match_player_duration_and_own_history_is_scoped()
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service.PublishAsync(40, Mcq(), default);
        var result = await f.Service.RecordAsync(new() { PlayerId = 101, MatchId = 10,
            QuestionId = Mcq().QuestionId, TimeTakenSeconds = 4.2m, SelectedChoiceId = "q001-c0" }, default);
        Assert.False(result.IsCorrect);
        Assert.True(result.HistoryId > 0);
        f.Context.ChangeTracker.Clear();
        var history = await f.Service.HistoryAsync(1, 101, 10, 1, 50, default);
        var attempt = Assert.Single(history.Items);
        Assert.Equal("q001-c0", attempt.SelectedChoiceId);
        Assert.Equal(4.2m, attempt.TimeTakenSeconds);
        Assert.Equal(Mcq().QuestionId, attempt.QuestionId);
        await Assert.ThrowsAsync<GenericException>(() => f.Service.HistoryAsync(2, 101, null, 1, 50, default));
    }

    [Theory]
    [InlineData("foreign-choice")]
    [InlineData("foreign-player")]
    [InlineData("negative-time")]
    [InlineData("mixed-response")]
    public async Task Invalid_history_is_rejected_without_a_partial_attempt(string invalid)
    {
        await using var f = await Fixture.CreateAsync();
        await f.Service.PublishAsync(40, Mcq(), default);
        var request = new QuestionHistoryRequest { PlayerId = 101, MatchId = 10,
            QuestionId = Mcq().QuestionId, TimeTakenSeconds = 4.2m, SelectedChoiceId = "q001-c0" };
        if (invalid == "foreign-choice") request.SelectedChoiceId = "other-choice";
        if (invalid == "foreign-player") request.PlayerId = 102;
        if (invalid == "negative-time") request.TimeTakenSeconds = -1;
        if (invalid == "mixed-response") request.AnswerText = "atom";
        await Assert.ThrowsAsync<GenericException>(() => f.Service.RecordAsync(request, default));
        Assert.Equal(0, await f.Context.Set<QuestionHistory>().CountAsync());
        Assert.Equal(0, await f.Context.Set<MCQHistory>().CountAsync());
    }

    internal sealed class Fixture : IAsyncDisposable
    {
        public QuestionDataSqliteContext Context { get; }
        public QuestionDataService Service { get; }
        private Fixture(QuestionDataSqliteContext context) { Context = context; Service = new(context); }
        public static async Task<Fixture> CreateAsync(Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor? interceptor = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("Data Source=:memory:");
            if (interceptor != null) options.AddInterceptors(interceptor);
            var context = new QuestionDataSqliteContext(options.Options);
            await context.Database.OpenConnectionAsync();
            await context.Database.EnsureCreatedAsync();
            context.Users.AddRange(new User { Id = 40, UserName = "admin", Email = "admin@example.com", Name = "Admin", IsPlatformAdmin = true, Status = UserStatus.Active },
                new User { Id = 1, UserName = "one", Email = "one@example.com", Name = "One", Status = UserStatus.Active },
                new User { Id = 2, UserName = "two", Email = "two@example.com", Name = "Two", Status = UserStatus.Active });
            context.Players.AddRange(new Player { Id = 101, UserId = 1, Name = "One", Email = "one@example.com" },
                new Player { Id = 102, UserId = 2, Name = "Two", Email = "two@example.com" });
            context.Matches.Add(new Match { Id = 10, MatchCode = "question-test", StartedAt = DateTime.UtcNow, TotalPlayers = 1 });
            context.MatchPlayers.Add(new MatchPlayer { MatchId = 10, PlayerProfileId = 101 });
            await context.SaveChangesAsync();
            return new(context);
        }
        public async ValueTask DisposeAsync() => await Context.DisposeAsync();
    }
}
