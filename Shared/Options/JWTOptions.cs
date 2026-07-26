namespace Shared.Options
{
    public class JWTOptions
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int TokenLifetimeInMinutes { get; set; }
        public int AccessTokenLifetimeMinutes { get; set; } = 15;
        public int RefreshTokenLifetimeDays { get; set; } = 30;
        public int ClockSkewSeconds { get; set; } = 30;
    }
}
