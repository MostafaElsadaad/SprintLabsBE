using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Domain.Enums;
using Domain.Services;

using Shared.Enums;
using Shared.Exceptions;
using Shared.Responses;

namespace Infrastructure.Services
{
    public sealed class ApiSyncService : IApisSyncService
    {
        private readonly HttpClient _http;
        private readonly string _specPath;

        public ApiSyncService(HttpClient http, string specPath = "/compass/internal-docs/openapi.json")
        {
            _http = http;
            _specPath = specPath;
        }

        public async Task<HttpResponseMessage> SyncApiAsync(string environmentUrl, string? knownEtag)
        {
            var baseUri = new Uri(environmentUrl, UriKind.Absolute);
            var requestUri = new Uri(baseUri, _specPath);

            using var req = new HttpRequestMessage(HttpMethod.Get, requestUri);

            req.Headers.TryAddWithoutValidation("If-None-Match", knownEtag);

            var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            return resp;
        }


        
    }
}