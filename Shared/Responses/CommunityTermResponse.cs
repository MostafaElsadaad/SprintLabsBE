namespace Shared.Responses;

public class CommunityTermResponse
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string ProgressDefinition => "Elapsed UTC calendar time from startDate midnight through the midnight after inclusive endDate, clamped to 0-100% and rounded to two decimals. This is not curriculum completion.";
}
