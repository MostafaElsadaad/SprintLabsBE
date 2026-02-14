using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Shared.Responses
{
    public class APIResponse
    {
        public string RawJson { get; set; }
        public string Etag { get; set; }
    }
}
