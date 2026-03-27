using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Shared.Responses
{
    public class GoogleUserResponse
    {
        [JsonProperty("sub")]
        public string Sub { get; set; } // Google user ID

        [JsonProperty("email")]
        public string Email { get; set; } // User email

        [JsonProperty("name")]
        public string Name { get; set; } // Full name

        [JsonProperty("picture")]
        public string Picture { get; set; } // Profile picture URL 
        [JsonProperty("hd")]
        public string Domain { get; set; }
    }
}

