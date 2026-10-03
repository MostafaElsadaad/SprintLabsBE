using Shared.Responses;
namespace Infrastructure.Services;
internal class MissionBoxSnapshot
{
    public long ShopProductId { get; set; }
    public string Name { get; set; } = "";
    public List<MissionItemResponse> Items { get; set; } = new();
}
