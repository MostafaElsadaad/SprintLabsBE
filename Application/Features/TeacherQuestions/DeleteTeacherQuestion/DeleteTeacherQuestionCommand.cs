using MediatR;

namespace Application.Features.TeacherQuestions.DeleteTeacherQuestion;

public class DeleteTeacherQuestionCommand : IRequest<bool>
{
    public long AuthenticatedUserId { get; set; }
    public long TeacherQuestionId { get; set; }
}
