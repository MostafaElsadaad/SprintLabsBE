using MediatR;
using Shared.Requests.LiveGames;
using Shared.Responses.LiveGames;

namespace Application.Features.TeacherQuestions.SaveTeacherQuestion;

/// <summary>Creates a private question (TeacherQuestionId null) or updates one the caller owns.</summary>
public class SaveTeacherQuestionCommand : IRequest<TeacherQuestionResponse>
{
    public long AuthenticatedUserId { get; set; }
    public long? TeacherQuestionId { get; set; }
    public TeacherQuestionRequest Request { get; set; } = new();
}
