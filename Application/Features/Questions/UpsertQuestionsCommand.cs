using System.Text.Json;
using MediatR;

namespace Application.Features.Questions
{
    public class UpsertQuestionsCommand : IRequest<GetQuestionsDto>
    {
        public int Grade { get; set; }
        public int? Assignment { get; set; }
        public JsonElement PayloadJson { get; set; }
    }
}
