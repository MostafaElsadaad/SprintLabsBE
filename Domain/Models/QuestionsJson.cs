using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class QuestionsJson
    {
        public long Id { get; set; }

        // Indexed
        public int Grade { get; set; }

        // Nullable = “general pool” questions or not tied to an assignment
        public int? Assignment { get; set; }

        // Big JSON blob (question pack)
        public string PayloadJson { get; set; }

        // Optional: versioning / cache-busting
        public int Version { get; set; } = 1;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
