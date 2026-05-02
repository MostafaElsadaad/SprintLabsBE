using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Accounts.GoogleAuthenticate
{
    public class GoogleAuthResponse
    {
        // Identity
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string AvatarUrl { get; set; }

        // Optional profile
        public int? Age { get; set; }
        public string? Grade { get; set; }
        public string? SchoolName { get; set; }

        // Game progression ← what Mostafa wants included
        public int Gold { get; set; }
        public int Experience { get; set; }
        public int Level { get; set; }

        // Auth
        public string Token { get; set; }
    }
}
