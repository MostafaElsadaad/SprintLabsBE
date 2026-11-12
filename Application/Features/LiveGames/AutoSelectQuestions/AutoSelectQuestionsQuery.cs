using MediatR;
using Shared.Requests.LiveGames;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.AutoSelectQuestions;

public class AutoSelectQuestionsQuery : IRequest<AutoSelectQuestionsResponse>
{
    public long AuthenticatedUserId { get; set; }
    public AutoSelectQuestionsRequest Request { get; set; } = new();
}
