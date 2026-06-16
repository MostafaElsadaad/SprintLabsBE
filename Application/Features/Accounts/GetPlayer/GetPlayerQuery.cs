using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;



using MediatR;

using Shared.Responses;
namespace Application.Features.Accounts.GetPlayer
{
    public class GetPlayerQuery : IRequest<BaseResponse<PlayerProfileResponse>>
    {
     
        [JsonIgnore]
        public string? GoogleId { get; set; } = default!;
    }
}
