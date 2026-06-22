namespace Application.Features.Admin.Communities.CreateCommunity;

public class CreateCommunityRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
