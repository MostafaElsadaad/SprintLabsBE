using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Domain.Enums;
using Shared.Responses;

namespace Domain.Services
{
    public interface IApisSyncService
    {
        public Task<HttpResponseMessage> SyncApiAsync(string environmentUrl, string? knownEtag);
    }
}
