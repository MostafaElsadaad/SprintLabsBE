using System.Text.Json;
using Application.Features.LiveGames.AutoSelectQuestions;
using Application.Features.LiveGames.CreateLiveGame;
using Application.Features.LiveGames.GetLiveGameQuestions;
using Application.Features.LiveGames.ListLessons;
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

    private static QuestionDataRequest McqWithChoices(string text, int choices)
    {
        var question = Mcq(text);
        question.Choices = Enumerable.Range(0, choices)
            .Select(i => new QuestionChoiceData { ChoiceId = Guid.NewGuid().ToString("N"), ChoiceText = "choice " + i, IsCorrect = i == 0 })
            .ToList();
        return question;
    }

    [Fact]
    public async Task Questions_with_more_answers_than_the_game_shows_are_rejected_and_never_auto_picked()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var wide = McqWithChoices("Five choices", 5);
        var fits = McqWithChoices("Four choices", 4);
        await f.Service.PublishAsync(Admin, wide, default);
        await f.Service.PublishAsync(Admin, fits, default);

        var picked = await new AutoSelectQuestionsQueryHandler(Users(), f.Service).Handle(new AutoSelectQuestionsQuery
            { AuthenticatedUserId = Admin, Request = new() { Curriculum = "Egypt", Grade = 7, Unit = 1, Lesson = 1, Count = 2 } }, default);
        picked.Questions.Select(q => q.Question.QuestionId).Should().Equal(fits.QuestionId);

        var saveSet = () => new SaveLiveGameSetCommandHandler(Users(), new BaseRepository<LiveGameQuestionSet>(f.Context), f.Service)
            .Handle(new SaveLiveGameSetCommand { AuthenticatedUserId = Admin, Request = new()
                { Name = "Wide", Curriculum = "Egypt", Grade = 7, Questions = new() { new() { Source = "mine", Question = McqWithChoices("Mine, five", 5) } } } }, default);
        (await saveSet.Should().ThrowAsync<GenericException>()).Which.Message.Should().StartWith("Question 1: Use at most 4 choices");

        var saveMine = () => new SaveTeacherQuestionCommandHandler(Users(), new BaseRepository<TeacherQuestion>(f.Context), f.Service)
            .Handle(new SaveTeacherQuestionCommand { AuthenticatedUserId = Admin, Request = new() { Question = McqWithChoices("Mine, six", 6) } }, default);
        (await saveMine.Should().ThrowAsync<GenericException>()).Which.Message.Should().Contain("at most 4 choices");
    }

    [Theory]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public async Task A_game_allows_up_to_30_players(int maxPlayers, bool allowed)
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var create = () => new CreateLiveGameCommandHandler(Users(), new BaseRepository<LiveGame>(f.Context), f.Service, new Infrastructure.Services.LiveGameStateStore())
            .Handle(new CreateLiveGameCommand { AuthenticatedUserId = Admin, Request = new()
                { Title = "Big class", Curriculum = "Egypt", Grade = 7, MaxPlayers = maxPlayers, Questions = new() { new() { Source = "mine", Question = TrueFalse("Big class question") } } } }, default);
        if (allowed) (await create()).MaxPlayers.Should().Be(30);
        else await create.Should().ThrowAsync<GenericException>();
    }

    private static QuestionDataRequest At(string subject, int unit, int lesson)
    {
        var question = Mcq($"{subject} U{unit} L{lesson}");
        question.Subject = subject;
        question.Unit = unit;
        question.Lesson = lesson;
        return question;
    }

    [Fact]
    public async Task Several_subjects_units_and_lessons_can_be_chosen_together()
    {
        await using var f = await QuestionDataServiceTests.Fixture.CreateAsync();
        var scienceU1L1 = At("Science", 1, 1);
        var scienceU1L2 = At("Science", 1, 2);
        var scienceU2L1 = At("Science", 2, 1);
        var scienceU2L3 = At("Science", 2, 3);
        var mathU2L3 = At("Math", 2, 3);
        var mathU3L1 = At("Math", 3, 1);
        var artU1L1 = At("Art", 1, 1);
        foreach (var q in new[] { scienceU1L1, scienceU1L2, scienceU2L1, scienceU2L3, mathU2L3, mathU3L1, artU1L1 })
            await f.Service.PublishAsync(Admin, q, default);

        // Lessons are listed per unit, across the chosen subjects only.
        var lessons = await new ListLessonsQueryHandler(Users(), f.Service).Handle(new ListLessonsQuery
            { AuthenticatedUserId = Admin, Curriculum = "Egypt", Grade = 7, Subjects = new() { "Science", "Math" }, Units = new() { 1, 2 } }, default);
        lessons.Select(l => (l.Unit, l.Lesson, l.QuestionCount)).Should().Equal((1, 1, 1), (1, 2, 1), (2, 1, 1), (2, 3, 2));

        // Science + Math; unit 1 limited to lesson 2, unit 2 limited to lesson 3, unit 3 whole.
        var picked = await new AutoSelectQuestionsQueryHandler(Users(), f.Service).Handle(new AutoSelectQuestionsQuery
        {
            AuthenticatedUserId = Admin,
            Request = new()
            {
                Curriculum = "Egypt", Grade = 7, Count = 10,
                Subjects = new() { "Science", "Math" },
                Units = new() { 1, 2, 3 },
                Lessons = new() { new() { Unit = 1, Lesson = 2 }, new() { Unit = 2, Lesson = 3 }, new() { Unit = 9, Lesson = 1 } }
            }
        }, default);
        picked.Questions.Select(q => q.Question.QuestionId).Should().BeEquivalentTo(new[]
            { scienceU1L2.QuestionId, scienceU2L3.QuestionId, mathU2L3.QuestionId, mathU3L1.QuestionId });

        // The scope is stored with a game and a saved set; single columns stay empty when several are chosen.
        var game = await new CreateLiveGameCommandHandler(Users(), new BaseRepository<LiveGame>(f.Context), f.Service, new Infrastructure.Services.LiveGameStateStore())
            .Handle(new CreateLiveGameCommand { AuthenticatedUserId = Admin, Request = new()
            {
                Title = "Mixed", Curriculum = "Egypt", Grade = 7, MaxPlayers = 4,
                Subjects = new() { "Math", "Science" }, Units = new() { 2, 1 },
                Lessons = new() { new() { Unit = 2, Lesson = 3 } },
                Questions = picked.Questions
            } }, default);
        game.Subject.Should().BeNull();
        game.Unit.Should().BeNull();
        game.Subjects.Should().Equal("Math", "Science");
        game.Units.Should().Equal(1, 2);
        game.Lessons.Select(l => (l.Unit, l.Lesson)).Should().Equal((2, 3));

        var set = await new SaveLiveGameSetCommandHandler(Users(), new BaseRepository<LiveGameQuestionSet>(f.Context), f.Service)
            .Handle(new SaveLiveGameSetCommand { AuthenticatedUserId = Admin, Request = new()
            {
                Name = "Single", Curriculum = "Egypt", Grade = 7,
                Subjects = new() { "Science" }, Units = new() { 2 }, Lessons = new() { new() { Unit = 2, Lesson = 3 } },
                Questions = new() { new() { Source = "library", Question = scienceU2L3 } }
            } }, default);
        (set.Subject, set.Unit, set.Lesson).Should().Be(("Science", 2, 3));
        set.Lessons.Select(l => (l.Unit, l.Lesson)).Should().Equal((2, 3));
    }
}
