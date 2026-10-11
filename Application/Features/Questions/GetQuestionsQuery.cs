using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Shared.Requests.QuestionData;

namespace Application.Features.Questions
{
    public class GetQuestionsQuery : IRequest<GetQuestionsDto>
    {
        public int Grade { get; set; }
        public int? Assignment { get; set; }
        public QuestionBankFilter? Filter { get; set; }
    }
}
