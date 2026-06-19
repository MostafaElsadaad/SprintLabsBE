using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Domain.Models;
using Domain.Repositories;
using MediatR;
using Shared.Enums;
using Shared.Exceptions;

namespace Application.Features.Questions
{
    public class UpsertQuestionsCommandHandler : IRequestHandler<UpsertQuestionsCommand, GetQuestionsDto>
    {
        private readonly IBaseRepository<QuestionsJson> _questionsRepository;

        public UpsertQuestionsCommandHandler(IBaseRepository<QuestionsJson> questionsRepository)
        {
            _questionsRepository = questionsRepository;
        }

        public async Task<GetQuestionsDto> Handle(UpsertQuestionsCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate Grade
            if (request.Grade < 1 || request.Grade > 20)
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidInput,
                    statusCode: HttpStatusCode.BadRequest,
                    errorCode: ErrorCode.Failure);
            }

            // 2. Validate PayloadJson (must be object)
            if (request.PayloadJson.ValueKind != JsonValueKind.Object)
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidInput,
                    statusCode: HttpStatusCode.BadRequest,
                    errorCode: ErrorCode.Failure);
            }

            // 3. Validate Questions property (must be array and not empty)
            if (!request.PayloadJson.TryGetProperty("Questions", out var questionsProperty) ||
                questionsProperty.ValueKind != JsonValueKind.Array)
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidInput,
                    statusCode: HttpStatusCode.BadRequest,
                    errorCode: ErrorCode.Failure);
            }

            if (questionsProperty.GetArrayLength() == 0)
            {
                throw new GenericException(
                    message: ErrorMessage.InvalidInput,
                    statusCode: HttpStatusCode.BadRequest,
                    errorCode: ErrorCode.Failure);
            }

            // 4. Find existing record by Grade
            var existing = await _questionsRepository.GetByCustomConditionAsync(x => x.Grade == request.Grade);

            // Serialize request.PayloadJson to string
            string payloadString = JsonSerializer.Serialize(request.PayloadJson);

            QuestionsJson entity;
            if (existing == null)
            {
                entity = new QuestionsJson
                {
                    Grade = request.Grade,
                    Assignment = request.Assignment,
                    PayloadJson = payloadString,
                    Version = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _questionsRepository.AddAsync(entity);
            }
            else
            {
                entity = existing;
                entity.PayloadJson = payloadString;
                entity.Assignment = request.Assignment;
                entity.Version += 1;
                entity.UpdatedAt = DateTime.UtcNow;
                await _questionsRepository.UpdateAsync(entity);
            }

            await _questionsRepository.SaveChangesAsync();

            // Convert stored JSON back to JsonElement using .Clone() to safely return it
            using var doc = JsonDocument.Parse(entity.PayloadJson);
            var payload = doc.RootElement.Clone();

            return new GetQuestionsDto
            {
                Id = entity.Id,
                Grade = entity.Grade,
                Assignment = entity.Assignment,
                PayloadJson = payload,
                Version = entity.Version,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
