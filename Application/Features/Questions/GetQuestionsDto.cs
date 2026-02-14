using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Features.Questions
{
    public class GetQuestionsDto
    {
        public long Id { get; set; }
        public int Grade { get; set; }

        public int? Assignment { get; set; }

        // Big JSON blob (question pack)
        public JsonElement PayloadJson { get; set; }

        public int Version { get; set; } = 1;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
