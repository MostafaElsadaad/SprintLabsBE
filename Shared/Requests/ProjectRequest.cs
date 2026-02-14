using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shared.Responses;

namespace Shared.Requests
{
    public class ProjectRequest : PagedRequest
    {
        public int? SquadId { get; set; }
    }
}
