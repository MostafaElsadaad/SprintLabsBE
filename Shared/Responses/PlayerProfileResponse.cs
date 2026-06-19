using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses
{
    public class PlayerProfileResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? PictureUrl { get; set; }

        public int Gold { get; set; }
        public int Experience { get; set; }
        public int Level { get; set; }
        public string? SchoolName { get; set; }
        public int? Age { get; set; }
        public int? Grade { get; set; }

    }
}
