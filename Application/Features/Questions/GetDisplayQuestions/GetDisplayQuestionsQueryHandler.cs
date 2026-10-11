using System.Net;
using Domain.Services;
using MediatR;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Requests.QuestionData;
using Shared.Responses;

namespace Application.Features.Questions.GetDisplayQuestions;

public class GetDisplayQuestionsQueryHandler(IQuestionDataService service, IUserService users)
    : IRequestHandler<GetDisplayQuestionsQuery, ProgressionPage<QuestionDisplayResponse>>
{
    public async Task<ProgressionPage<QuestionDisplayResponse>> Handle(GetDisplayQuestionsQuery request, CancellationToken cancellationToken)
    {
        var user = await users.GetCurrentUser(request.UserId);
        if (user == null || user.Status != "Active")
            throw new GenericException(ErrorCode.Failure, "Active account required.", HttpStatusCode.Forbidden);
        var bank = await service.GetBankAsync(request.Filter, cancellationToken);
        return new() { Items = bank.Items.Select(Display).ToList(), Total = bank.Total, Page = bank.Page, PageSize = bank.PageSize };
    }

    private static QuestionDisplayResponse Display(QuestionDataRequest q) => new()
    {
        QuestionId = q.QuestionId, Curriculum = q.Curriculum, Grade = q.Grade, Language = q.Language,
        Subject = q.Subject, Term = q.Term, Unit = q.Unit, Lesson = q.Lesson, QuestionType = q.QuestionType, QuestionText = q.QuestionText, TimerSeconds = q.TimerSeconds,
        Choices = q.Choices.Select(x => new QuestionDisplayOption { Id = x.ChoiceId, Text = x.ChoiceText }).ToList(),
        Items = q.Items.Select(x => new QuestionDisplayOption { Id = x.ItemId, Text = x.ItemText }).ToList(),
        Options = q.Options.Select(x => new QuestionDisplayOption { Id = x.OptionId, Text = x.OptionText }).ToList(),
        // Independent text order and side-local IDs do not expose stored correct pair associations.
        MatchingLeft = q.Pairs.OrderBy(x => x.LeftText, StringComparer.Ordinal).ThenBy(x => x.PairId, StringComparer.Ordinal)
            .Select((x, i) => new QuestionDisplayOption { Id = $"left-{i}", Text = x.LeftText }).ToList(),
        MatchingRight = q.Pairs.OrderBy(x => x.RightText, StringComparer.Ordinal).ThenBy(x => x.PairId, StringComparer.Ordinal)
            .Select((x, i) => new QuestionDisplayOption { Id = $"right-{i}", Text = x.RightText }).ToList(),
        BlankCount = q.QuestionType == "FillBlank" ? 1 : q.BlankAnswers.Count
    };
}
