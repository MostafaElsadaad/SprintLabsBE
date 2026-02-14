
using System.Globalization;

using Domain.Models;

using Microsoft.AspNetCore.Identity;

namespace Infrastructure.DataAccess
{
    public class User : IdentityUser<long>
    {
        public int? UserProfileId { get; set; }
        //public virtual UserProfile UserProfile { get; set; }

    }
}