namespace Application.Features.Admin.Communities.Common;

public class OwnerSummaryResponse
{
    public long UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
