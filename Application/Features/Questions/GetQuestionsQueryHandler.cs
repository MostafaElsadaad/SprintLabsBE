using System.Net;
using Domain.Services;
using MediatR;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Requests.QuestionData;

namespace Application.Features.Questions;

public class GetQuestionsQueryHandler(IQuestionDataService questionData)
    : IRequestHandler<GetQuestionsQuery, GetQuestionsDto>
{
    public async Task<GetQuestionsDto> Handle(GetQuestionsQuery request, CancellationToken cancellationToken)
    {
        if (request.Assignment.HasValue)
            throw new GenericException(ErrorCode.Failure,
                "Assignment selection is no longer supported. Use curriculum, grade, unit and lesson filters.",
                HttpStatusCode.BadRequest);

        var filter = request.Filter ?? new QuestionBankFilter { Grade = request.Grade };
        var payload = await questionData.ProjectBankAsync(filter, cancellationToken)
            ?? throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);

        // Preserve the transport envelope without inventing persisted pack identity or timestamps.
        return new GetQuestionsDto
        {
            Id = 0,
            Grade = filter.Grade,
            Assignment = null,
            PayloadJson = payload,
            Version = 1,
            CreatedAt = DateTime.UnixEpoch,
            UpdatedAt = null
        };
    }
}
