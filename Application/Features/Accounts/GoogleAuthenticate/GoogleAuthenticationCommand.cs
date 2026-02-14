using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;

using Shared.Responses;

namespace Application.Features.Accounts.GoogleAuthenticate
{
    public class GoogleAuthenticationCommand : IRequest<LoginResponse>
    {
        public string AccessToken { get; set; }
    }
}
