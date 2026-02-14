using System.Text.Json.Serialization;

namespace Shared.Options
{
    public class BitbucketOptions
    {
        [JsonIgnore]
        public required string AccessToken { get; set; }
        public required string BaseUrl { get; set; }
    }
}