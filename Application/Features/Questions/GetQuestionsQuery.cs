using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;

namespace Application.Features.Questions
{
    public class GetQuestionsQuery : IRequest<GetQuestionsDto>
    {
        public int Grade { get; set; }
        public int? Assignment { get; set; }
    }
}
