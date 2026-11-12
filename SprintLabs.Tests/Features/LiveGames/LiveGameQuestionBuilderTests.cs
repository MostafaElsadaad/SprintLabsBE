using System.Text.Json;
using Application.Features.LiveGames.AutoSelectQuestions;
using Application.Features.LiveGames.CreateLiveGame;
using Application.Features.LiveGames.GetLiveGameQuestions;
using Application.Features.LiveGameSets.SaveLiveGameSet;
using Application.Features.TeacherQuestions.SaveTeacherQuestion;
using Compass.Tests.Features.QuestionData;
using Domain.Models;
using Domain.Services;
using FluentAssertions;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Moq;
using Shared.Exceptions;
using Shared.Requests.LiveGames;
using Shared.Requests.QuestionData;
using Shared.Responses;

namespace Compass.Tests.Features.LiveGames;

public class LiveGameQuestionBuilderTests
{
    private const long Admin = 40;

    private static IUserService Users()
    {
        var users = new Mock<IUserService>();
        users.Setup(u => u.GetCurrentUser(Admin)).ReturnsAsync(new UserIdentityResponse { Id = Admin, IsPlatformAdmin = true, Status = "Active" });
        return users.Object;
    }

    private static QuestionDataRequest Mcq(string text, int lesson = 1) => new()
    {
        QuestionId = Guid.NewGuid(), Curriculum = "Egypt", Grade = 7, Language = "en", Subject = "Science", Term = 1, Unit = 1, Lesson = lesson,
        QuestionType = "MCQ", QuestionText = text,
        Choices = new()
        {
            new() { ChoiceId = Guid.NewGuid().ToString("N"), ChoiceText = "wrong", IsCorrect = false },
            new() { ChoiceId = Guid.NewGuid().ToString("N"), ChoiceText = "right", IsCorrect = true }
        }
    };

    private static QuestionDataRequest TrueFalse(string text) => new()
    {
        QuestionId = Guid.NewGuid(), Curriculum = "Egypt", Grade = 7, Language = "en", Subject = "Science", Term = 1, Unit = 1, Lesson = 1,
        QuestionType = "TrueOrFalse", QuestionText = text, CorrectAnswer = true
    };

    [Fact]
    public async Task Auto_select_balances_types_excludes_ids_and_warns_when_the_lesson_is_short()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var published = new List<QuestionDataRequest>();
        for (int i = 0; i < 3; i++) published.Add(Mcq("MCQ " + i));
        for (int i = 0; i < 3; i++) published.Add(TrueFalse("TF " + i));
        published.Add(Mcq("Other lesson", lesson: 2));
        foreach (var q in published) await f.Service.PublishAsync(Admin, q, default);
        var handler = new AutoSelectQuestionsQueryHandler(Users(), f.Service);

        var four = await handler.Handle(new AutoSelectQuestionsQuery { AuthenticatedUserId = Admin, Request = new()
            { Curriculum = "Egypt", Grade = 7, Unit = 1, Lesson = 1, Count = 4 } }, default);
        four.Questions.Should().HaveCount(4);
        four.Questions.GroupBy(q => q.Question.QuestionType).Select(g => g.Count()).Should().AllBeEquivalentTo(2);
        four.Questions.Should().OnlyContain(q => q.Source == "library" && q.Question.Lesson == 1);
        four.Warning.Should().BeNull();

        var replacement = await handler.Handle(new AutoSelectQuestionsQuery { AuthenticatedUserId = Admin, Request = new()
            { Curriculum = "Egypt", Grade = 7, Unit = 1, Lesson = 1, Count = 1, ExcludeQuestionIds = published.Take(5).Select(q => q.QuestionId).ToList() } }, default);
        replacement.Questions.Single().Question.QuestionId.Should().Be(published[5].QuestionId);

        var tooMany = await handler.Handle(new AutoSelectQuestionsQuery { AuthenticatedUserId = Admin, Request = new()
            { Curriculum = "Egypt", Grade = 7, Unit = 1, Lesson = 1, Count = 10 } }, default);
        tooMany.Questions.Should().HaveCount(6);
        tooMany.Warning.Should().Contain("Only 6");
    }

    [Fact]
    public async Task A_game_keeps_order_tracks_only_unchanged_library_questions_and_rejects_duplicates()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var library = Mcq("Library question");
        var toEdit = Mcq("Will be edited");
        await f.Service.PublishAsync(Admin, library, default);
        await f.Service.PublishAsync(Admin, toEdit, default);

        var mine = await new SaveTeacherQuestionCommandHandler(Users(), new BaseRepository<TeacherQuestion>(f.Context), f.Service)
            .Handle(new SaveTeacherQuestionCommand { AuthenticatedUserId = Admin, Request = new() { ThinkingSkill = "apply", Question = TrueFalse("My own question") } }, default);
        mine.ThinkingSkill.Should().Be("Apply");

        var edited = JsonSerializer.Deserialize<QuestionDataRequest>(JsonSerializer.Serialize(toEdit))!;
        edited.QuestionText = "Edited for this game only";
        var items = new List<LiveGameQuestionItem>
        {
            new() { Source = "mine", TeacherQuestionId = mine.Id, Question = mine.Question },
            new() { Source = "library", Question = library },
            new() { Source = "library", Question = edited }
        };

        var games = new BaseRepository<LiveGame>(f.Context);
        var created = await new CreateLiveGameCommandHandler(Users(), games, f.Service, new Infrastructure.Services.LiveGameStateStore())
            .Handle(new CreateLiveGameCommand { AuthenticatedUserId = Admin, Request = new()
                { Title = "Builder", Curriculum = "Egypt", Grade = 7, MaxPlayers = 4, Questions = items } }, default);
        created.QuestionCount.Should().Be(3);
        created.Questions.Select(q => q.Source).Should().Equal("mine", "library", "edited");

        // The shared original is untouched.
        (await f.Service.GetQuestionsByIdAsync(new[] { toEdit.QuestionId }, default)).Single().QuestionText.Should().Be("Will be edited");

        var payload = (await new GetLiveGameQuestionsQueryHandler(games, f.Service)
            .Handle(new GetLiveGameQuestionsQuery { Code = created.Code }, default)).PayloadJson;
        var ids = payload.GetProperty("Questions").EnumerateArray().Select(q => q.GetProperty("QuestionId").GetString()).ToList();
        ids.Should().Equal(Guid.Empty.ToString(), library.QuestionId.ToString(), Guid.Empty.ToString());

        var duplicate = new List<LiveGameQuestionItem> { new() { Question = library }, new() { Question = library } };
        var act = () => new SaveLiveGameSetCommandHandler(Users(), new BaseRepository<LiveGameQuestionSet>(f.Context), f.Service)
            .Handle(new SaveLiveGameSetCommand { AuthenticatedUserId = Admin, Request = new()
                { Name = "Dup", Curriculum = "Egypt", Grade = 7, Questions = duplicate } }, default);
        (await act.Should().ThrowAsync<GenericException>()).Which.Message.Should().Contain("already in this game");
    }

    [Fact]
    public async Task A_question_without_a_correct_answer_is_rejected_with_its_position()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var broken = Mcq("No correct choice");
        broken.Choices.ForEach(c => c.IsCorrect = false);
        var act = () => new SaveLiveGameSetCommandHandler(Users(), new BaseRepository<LiveGameQuestionSet>(f.Context), f.Service)
            .Handle(new SaveLiveGameSetCommand { AuthenticatedUserId = Admin, Request = new()
                { Name = "Broken", Curriculum = "Egypt", Grade = 7, Questions = new() { new() { Source = "mine", Question = broken } } } }, default);
        (await act.Should().ThrowAsync<GenericException>()).Which.Message.Should().StartWith("Question 1:");
    }
}
