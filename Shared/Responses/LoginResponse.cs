using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public long UserId { get; set; }
        public long? PlayerProfileId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PictureUrl { get; set; } = string.Empty;

        public int Gold { get; set; }
        public int Experience { get; set; }
        public int Level { get; set; }
    }
}