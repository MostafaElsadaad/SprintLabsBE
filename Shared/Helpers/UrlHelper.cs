using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Helpers
{
    public static class UrlHelper
    {
        private static readonly string _baseUrl = "https://storage.googleapis.com/compass-staging";

        public static string BuildUrl(string relativePath)
        {
            return $"{_baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        }
    }

}
