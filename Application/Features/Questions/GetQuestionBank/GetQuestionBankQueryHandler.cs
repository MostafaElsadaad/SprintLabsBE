using System.Net;
using Domain.Services;
using MediatR;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Requests.QuestionData;
using Shared.Responses;

namespace Application.Features.Questions.GetQuestionBank;

public class GetQuestionBankQueryHandler(IQuestionDataService service, IUserService users)
    : IRequestHandler<GetQuestionBankQuery, ProgressionPage<QuestionDataRequest>>
{
    public async Task<ProgressionPage<QuestionDataRequest>> Handle(GetQuestionBankQuery request, CancellationToken cancellationToken)
    {
        if (!request.IsGameServer)
        {
            var user = await users.GetCurrentUser(request.UserId);
            if (user == null || user.Status != "Active" || !user.IsPlatformAdmin)
                throw new GenericException(ErrorCode.Failure, "Platform administrator access required.", HttpStatusCode.Forbidden);
        }
        return await service.GetBankAsync(request.Filter, cancellationToken);
    }
}
