namespace Shared.Options;

public class EmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public bool EnableSsl { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "SprintLabs";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int TimeoutMilliseconds { get; set; } = 10000;
}
