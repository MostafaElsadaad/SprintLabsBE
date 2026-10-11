using System.Text.Json;
using System.Text.Json.Nodes;
using Shared.Requests.QuestionData;
using Shared.Exceptions;
using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Compass.Tests.Features.QuestionData;

public class QuestionDataSecondsTests
{
    [Fact]
    public void Timing_upgrade_preserves_existing_elapsed_values_instead_of_dropping_the_column()
    {
        var migration = new QuestionTimingSeconds { ActiveProvider = "Pomelo.EntityFrameworkCore.MySql" };
        Assert.DoesNotContain(migration.UpOperations, x => x is DropColumnOperation);
        Assert.Contains(migration.UpOperations.OfType<RenameColumnOperation>(), x => x.Name == "TimeTakenMs" && x.NewName == "TimeTakenSeconds");
        Assert.Contains(migration.UpOperations.OfType<SqlOperation>(), x => x.Sql.Contains("/ 1000"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.0001")]
    [InlineData("10000000")]
    public async Task Invalid_timer_is_rejected_without_rounding_or_partial_publication(string seconds)
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var q = QuestionDataTypesTests.Question("TrueOrFalse");
        q.TimerSeconds = decimal.Parse(seconds, System.Globalization.CultureInfo.InvariantCulture);
        await Assert.ThrowsAsync<GenericException>(() => f.Service.PublishAsync(40, q, default));
    }

    [Fact]
    public async Task Question_time_limit_round_trips_in_fractional_seconds()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var json = JsonSerializer.SerializeToNode(QuestionDataTypesTests.Question("TrueOrFalse"))!;
        json["TimerSeconds"] = 7.25m;
        var question = json.Deserialize<QuestionDataRequest>()!;
        await f.Service.PublishAsync(40, question, default);
        var stored = Assert.Single((await f.Service.GetBankAsync(new() { Grade = 7 }, default)).Items);
        var response = JsonSerializer.SerializeToElement(stored);
        Assert.True(response.TryGetProperty("TimerSeconds", out var timer));
        Assert.Equal(7.25m, timer.GetDecimal());
    }

    [Fact]
    public async Task History_keeps_fractional_seconds_without_millisecond_conversion()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var question = QuestionDataTypesTests.Question("TrueOrFalse");
        await f.Service.PublishAsync(40, question, default);
        var request = new JsonObject { ["QuestionId"] = question.QuestionId.ToString(), ["PlayerId"] = 101,
            ["MatchId"] = 10, ["SelectedAnswer"] = false, ["TimeTakenSeconds"] = 4.2m }.Deserialize<QuestionHistoryRequest>()!;
        var saved = await f.Service.RecordAsync(request, default);
        var response = JsonSerializer.SerializeToElement(saved);
        Assert.True(response.TryGetProperty("TimeTakenSeconds", out var time));
        Assert.Equal(4.2m, time.GetDecimal());
        Assert.False(response.TryGetProperty("TimeTakenMs", out _));
        f.Context.ChangeTracker.Clear();
        var read = Assert.Single((await f.Service.HistoryAsync(1, 101, 10, 1, 50, default)).Items);
        Assert.Equal(4.2m, JsonSerializer.SerializeToElement(read).GetProperty("TimeTakenSeconds").GetDecimal());
    }
}
