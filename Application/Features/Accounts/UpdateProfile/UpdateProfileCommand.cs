using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;



using MediatR;

using Shared.Responses;
namespace Application.Features.Accounts.UpdateProfile
{
    public class UpdateProfileCommand : IRequest<BaseResponse<PlayerProfileResponse>>
    {
        public string? Name { get; set; }
        public string? SchoolName { get; set; }
        public int? Grade { get; set; }
        public int? Age { get; set; }


        [JsonIgnore]
        public long UserId { get; set; }
    }
}
