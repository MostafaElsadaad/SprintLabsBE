namespace Shared.Requests;

public class CommunityTermRequest
{
    public string? Name { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
