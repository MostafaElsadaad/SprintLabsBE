using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Domain.Models;
using Domain.Repositories;
using Domain.Services;

using MediatR;

using Shared.Enums;
using Shared.Exceptions;
using System.Text.Json;

namespace Application.Features.Questions
{
    public class GetQuestionsQueryHandler : IRequestHandler<GetQuestionsQuery, GetQuestionsDto>
    {
        private readonly IBaseRepository<QuestionsJson> _questionsRepository;
        private readonly IQuestionDataService? _questionData;
        public GetQuestionsQueryHandler(IBaseRepository<QuestionsJson> questionsRepository, IQuestionDataService? questionData = null)
        {
            _questionsRepository = questionsRepository;
            _questionData = questionData;
        }

        public async Task<GetQuestionsDto> Handle(GetQuestionsQuery request, CancellationToken cancellationToken)
        {
            var question = await _questionsRepository.GetByCustomConditionAsync(x =>
                x.Grade == request.Grade &&
                (request.Assignment == null || x.Assignment == request.Assignment));

            var projected = _questionData == null ? null : request.Assignment == null
                ? await _questionData.ProjectBankAsync(request.Filter ?? new Shared.Requests.QuestionData.QuestionBankFilter { Grade = request.Grade }, cancellationToken)
                : question == null ? null : await _questionData.ProjectLegacyAsync(question, cancellationToken);

            var filter = request.Filter;
            var hasMetadataFilter = filter != null && (filter.Unit.HasValue || filter.Lesson.HasValue || filter.Term.HasValue ||
                filter.Curriculum != null || filter.Subject != null || filter.Language != null);
            if (request.Assignment == null && hasMetadataFilter && !projected.HasValue)
                throw new GenericException(ErrorCode.Failure, ErrorMessage.NotFound, HttpStatusCode.NotFound);

            if (question == null && !projected.HasValue)
                throw new GenericException(
                    message: ErrorMessage.NotFound,
                    statusCode: HttpStatusCode.NotFound,
                    errorCode: ErrorCode.Failure);

            using var doc = projected.HasValue ? null : JsonDocument.Parse(question!.PayloadJson);

            return new GetQuestionsDto
            {
                Id = question?.Id ?? 0,
                Grade = request.Grade,
                Assignment = question?.Assignment ?? request.Assignment,
                PayloadJson = projected ?? doc!.RootElement.Clone(),
                Version = question?.Version ?? 1,
                CreatedAt = question?.CreatedAt ?? DateTime.UnixEpoch,
                UpdatedAt = question?.UpdatedAt
            };


        }
    }
}
