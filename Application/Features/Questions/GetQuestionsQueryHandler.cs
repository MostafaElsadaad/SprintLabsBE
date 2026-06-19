using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Domain.Models;
using Domain.Repositories;

using MediatR;

using Shared.Enums;
using Shared.Exceptions;
using System.Text.Json;

namespace Application.Features.Questions
{
    public class GetQuestionsQueryHandler : IRequestHandler<GetQuestionsQuery, GetQuestionsDto>
    {
        private readonly IBaseRepository<QuestionsJson> _questionsRepository;
        public GetQuestionsQueryHandler(IBaseRepository<QuestionsJson> questionsRepository)
        {
            _questionsRepository = questionsRepository;
        }

        public async Task<GetQuestionsDto> Handle(GetQuestionsQuery request, CancellationToken cancellationToken)
        {
            var question = await _questionsRepository.GetByCustomConditionAsync(x =>
                x.Grade == request.Grade &&
                (request.Assignment == null || x.Assignment == request.Assignment));

            if (question == null)
                throw new GenericException(
                    message: ErrorMessage.NotFound,
                    statusCode: HttpStatusCode.NotFound,
                    errorCode: ErrorCode.Failure);

            var doc = JsonDocument.Parse(question.PayloadJson);

            return new GetQuestionsDto
            {
                Id = question.Id,
                Grade = question.Grade,
                Assignment = question.Assignment,
                PayloadJson = doc.RootElement,
                Version = question.Version,
                CreatedAt = question.CreatedAt,
                UpdatedAt = question.UpdatedAt
            };


        }
    }
}
