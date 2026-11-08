using MediatR;
using Shared.Responses.LiveGames;

namespace Application.Features.LiveGames.GetLiveGameQuestions;

public class GetLiveGameQuestionsQuery : IRequest<LiveGameQuestionsResponse>
{
    public string Code { get; set; } = "";
}
