namespace Shared.Responses;

public class ConfirmationDispatchResult
{
    public long UserId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? ConfirmationToken { get; set; }
    public DateTime? ResendAvailableAt { get; set; }
}
