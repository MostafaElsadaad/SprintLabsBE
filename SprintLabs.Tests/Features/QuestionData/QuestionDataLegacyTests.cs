using System.Text.Json;
using Application.Features.Questions;
using Domain.Models.QuestionData;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace Compass.Tests.Features.QuestionData;

public class QuestionDataLegacyTests
{
    [Fact]
    public async Task Equivalent_seconds_number_format_does_not_regenerate_legacy_question_ids()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var original = await f.Service.ImportLegacyAsync(40, 1, default);
        var pack = await f.Context.Questions.SingleAsync();
        var node = System.Text.Json.Nodes.JsonNode.Parse(pack.PayloadJson)!;
        node["Questions"]![0]!["MCQ"]!["Timer"] = 8;
        pack.PayloadJson = node.ToJsonString();
        await f.Context.SaveChangesAsync();
        var repeated = await f.Service.ImportLegacyAsync(40, 1, default);
        Assert.Equal(original.Select(x => x.QuestionId), repeated.Select(x => x.QuestionId));
        Assert.Equal(5, await f.Context.Set<Question>().CountAsync());
    }

    [Fact]
    public async Task Legacy_source_uuid_is_not_projected_when_published_content_does_not_match()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var q = QuestionDataTypesTests.Question("TrueOrFalse"); q.QuestionText = "Private question";
        await f.Service.PublishAsync(40, q, default);
        var pack = await f.Context.Questions.SingleAsync();
        pack.PayloadJson = $"{{\"Questions\":[{{\"QuestionId\":\"{q.QuestionId}\",\"Type\":6,\"TrueOrFalse\":{{\"Prompt\":\"Unknown answer\",\"CorrectAnswer\":true,\"Timer\":10}}}}]}}";
        Assert.Null(await f.Service.ProjectLegacyAsync(pack, default));
    }

    [Fact]
    public async Task Import_uses_dummy_metadata_keeps_seconds_and_is_idempotent_with_shared_id_export()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var exported = await f.Service.ImportLegacyAsync(40, 1, default);
        Assert.Equal(5, exported.Count);
        Assert.All(exported, q => { Assert.NotEqual(Guid.Empty, q.QuestionId); Assert.Equal("DUMMY", q.Curriculum);
            Assert.Equal("DUMMY", q.Subject); Assert.Equal("und", q.Language); Assert.Equal(5, q.Grade); Assert.Equal(1, q.Unit); });
        Assert.Equal(8m, exported.Single(q => q.QuestionType == "MCQ").TimerSeconds);
        Assert.Equal(10m, exported.Single(q => q.QuestionType == "Ordering").TimerSeconds);
        var repeated = await f.Service.ImportLegacyAsync(40, 1, default);
        Assert.Equal(exported.Select(q => q.QuestionId), repeated.Select(q => q.QuestionId));
        Assert.Equal(5, await f.Context.Set<Question>().CountAsync());
        Assert.Equal(1, await f.Context.Questions.CountAsync());
    }

    [Fact]
    public async Task Existing_get_response_is_rebuilt_from_relational_rows_with_same_pack_contract()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        await f.Service.ImportLegacyAsync(40, 1, default);
        var pack = await f.Context.Questions.SingleAsync();
        var projected = await f.Service.ProjectLegacyAsync(pack, default);
        Assert.True(projected.HasValue);
        var questions = projected.Value.GetProperty("Questions");
        Assert.Equal(5, questions.GetArrayLength());
        var mcq = questions[0].GetProperty("MCQ");
        Assert.Equal("What is 2+2?", mcq.GetProperty("Prompt").GetString());
        Assert.Equal(new[] { "3", "4", "5", "6" }, mcq.GetProperty("Choices").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal(1, mcq.GetProperty("CorrectIndex").GetInt32());
        Assert.Equal(8m, mcq.GetProperty("Timer").GetDecimal());
        Assert.True(questions[3].GetProperty("TrueOrFalse").GetProperty("CorrectAnswer").GetBoolean());
        Assert.True(Guid.TryParse(questions[0].GetProperty("QuestionId").GetString(), out _));
    }

    [Fact]
    public async Task Invalid_pack_rolls_back_every_question_and_keeps_legacy_payload()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var pack = await f.Context.Questions.SingleAsync();
        pack.PayloadJson = "{\"Questions\":[{\"Type\":6,\"TrueOrFalse\":{\"Prompt\":\"Valid\",\"Answer\":false,\"Timer\":8}},{\"Type\":4,\"ImageMCQ\":{\"Prompt\":\"Unsupported\"}}]}";
        await f.Context.SaveChangesAsync();
        var original = pack.PayloadJson;
        await Assert.ThrowsAsync<GenericException>(() => f.Service.ImportLegacyAsync(40, 1, default));
        Assert.Equal(0, await f.Context.Set<Question>().CountAsync());
        Assert.Equal(original, (await f.Context.Questions.SingleAsync()).PayloadJson);
    }
}
