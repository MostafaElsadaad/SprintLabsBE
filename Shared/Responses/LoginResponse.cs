using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses
{
    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PictureUrl { get; set; }

        public int Gold { get; set; }
        public int Experience { get; set; }
        public int Level { get; set; }
    }
}
