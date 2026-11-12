using MediatR;
using Shared.Requests.LiveGames;
using Shared.Responses.LiveGames;

namespace Application.Features.TeacherQuestions.ListTeacherQuestions;

public class ListTeacherQuestionsQuery : IRequest<List<TeacherQuestionResponse>>
{
    public long AuthenticatedUserId { get; set; }
    public TeacherQuestionFilter Filter { get; set; } = new();
}
