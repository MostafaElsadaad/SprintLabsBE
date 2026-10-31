using System.Security.Cryptography;
using System.Text.Json;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Responses.Shop;
namespace Infrastructure.Services;

public partial class ShopService
{
    private static readonly string[] Categories = { "Character", "Skin", "Pet", "Accessory", "Effect" };
    private static string? CategoryOf(Item item) => item.IsCharacter ? "Character" : item.Category;
    private static int SellPriceOf(Item item) => item.ItemKey == "quantum-scholar" || item.PrefabKey == "characters/quantum-scholar"
        || item.ItemKey == "king-of-knowledge" || item.PrefabKey == "characters/king-of-knowledge"
        ? 0 : item.IsCharacter && item.Category == null ? 50 : item.SellPrice;
    private static List<ShopCategoryOddsResponse> Odds(ShopProduct product)
    {
        List<ShopCategoryOddsResponse>? odds;
        try { odds = JsonSerializer.Deserialize<List<ShopCategoryOddsResponse>>(product.CategoryOddsJson ?? "null"); }
        catch (JsonException) { throw Conflict("Box odds unavailable."); }
        if (odds == null || odds.Count == 0 || odds.Any(x => !Categories.Contains(x.Category) || x.Percent is < 0 or > 100) ||
            odds.Select(x=>x.Category).Distinct().Count()!=odds.Count || odds.Sum(x=>x.Percent)!=100)
            throw Conflict("Box odds unavailable.");
        return odds;
    }
    private static ShopBoxResponse BoxView(ShopProduct p, int quantity) => new()
    {
        ProductId=p.Id, BoxKey=p.BoxKey!, Name=p.Name, IconKey=p.IconKey ?? "", DisplayLevel=p.DisplayLevel ?? 0,
        CoinPrice=p.CoinPrice ?? 0, IsActive=p.IsActive, QuantityOwned=quantity, Odds=Odds(p)
    };
    private async Task<T?> ReplayAsync<T>(Player player, Guid request, string action, long target, CancellationToken ct) where T:class
    {
        var key=request.ToString("N");
        if(await context.Set<ShopPurchase>().AnyAsync(x=>x.PlayerProfileId==player.Id && x.RequestId==key,ct))
            throw Conflict("requestId was already used for a direct purchase.");
        var saved=await context.Set<ShopOperation>().AsNoTracking().SingleOrDefaultAsync(x=>x.PlayerProfileId==player.Id && x.RequestId==key,ct);
        if(saved==null)return null;
        if(saved.Action!=action || saved.TargetId!=target)throw Conflict("requestId was already used for another action or target.");
        return JsonSerializer.Deserialize<T>(saved.ResultJson) ?? throw Conflict("Saved receipt unavailable.");
    }
    private void SaveOperation<T>(Player player, Guid request, string action, long target, T result)
    {
        player.UpdatedAt=DateTime.UtcNow;
        context.Set<ShopOperation>().Add(new() {PlayerProfileId=player.Id,RequestId=request.ToString("N"),Action=action,TargetId=target,
            ResultJson=JsonSerializer.Serialize(result),CreatedAt=DateTime.UtcNow});
    }
    internal static string SelectCategory(IReadOnlyList<ShopCategoryOddsResponse> odds,int roll)
    {
        if(roll is <0 or >=100)throw new ArgumentOutOfRangeException(nameof(roll));
        var cumulative=0;
        foreach(var odd in odds) { cumulative+=odd.Percent; if(roll<cumulative)return odd.Category; }
        throw Conflict("Box odds unavailable.");
    }
    private async Task<Dictionary<string,Item[]>> RewardPoolsAsync(List<ShopCategoryOddsResponse> odds,CancellationToken ct)
    {
        var pool=await context.Items.AsNoTracking().Where(x=>x.IsBoxReward).OrderBy(x=>x.Id).ToListAsync(ct);
        // Validate every enabled category before any mutation. Keys map exclusively to shipped assets.
        var pools=odds.Where(x=>x.Percent>0).ToDictionary(x=>x.Category,x=>pool.Where(i=>CategoryOf(i)==x.Category && BundledReward(i)).ToArray());
        if(pools.Values.Any(x=>x.Length==0))throw Conflict("Box reward catalogue unavailable.");
        return pools;
    }
    public async Task<ShopBoxPurchaseResponse> PurchaseBoxAsync(long userId,long productId,Guid requestId,CancellationToken ct)
    {
        if(productId<=0 || requestId==Guid.Empty)throw Invalid();
        return await AtomicAsync(userId,async player=>
        {
            var replay=await ReplayAsync<ShopBoxPurchaseResponse>(player,requestId,"BuyBox",productId,ct);
            if(replay!=null){replay.IsReplay=true;return replay;}
            var p=await context.ShopProducts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==productId && x.BoxKey!=null,ct);
            if(p==null || !p.IsActive || p.CoinPrice is not >0)throw Missing();
            _=await RewardPoolsAsync(Odds(p),ct);
            if(player.Gold<p.CoinPrice.Value)throw Conflict("Insufficient coins.");
            var stock=await context.Set<PlayerBox>().SingleOrDefaultAsync(x=>x.PlayerProfileId==player.Id && x.ShopProductId==productId,ct);
            if(stock==null){stock=new(){PlayerProfileId=player.Id,ShopProductId=productId};context.Add(stock);}
            stock.Quantity=checked(stock.Quantity+1);player.Gold-=p.CoinPrice.Value;
            var result=new ShopBoxPurchaseResponse{RequestId=requestId,ProductId=productId,CoinsSpent=p.CoinPrice.Value,CoinsAfter=player.Gold,QuantityOwned=stock.Quantity};
            SaveOperation(player,requestId,"BuyBox",productId,result);return result;
        },ct);
    }
    public async Task<ShopBoxOpenResponse> OpenBoxAsync(long userId,long productId,Guid requestId,CancellationToken ct)
    {
        if(productId<=0 || requestId==Guid.Empty)throw Invalid();
        return await AtomicAsync(userId,async player=>
        {
            var replay=await ReplayAsync<ShopBoxOpenResponse>(player,requestId,"OpenBox",productId,ct);
            if(replay!=null){replay.IsReplay=true;return replay;}
            var p=await context.ShopProducts.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==productId && x.BoxKey!=null,ct);
            if(p==null)throw Missing();
            var stock=await context.Set<PlayerBox>().SingleOrDefaultAsync(x=>x.PlayerProfileId==player.Id && x.ShopProductId==productId,ct);
            if(stock?.Quantity is not >0)throw Conflict("Box is not owned.");
            var odds=Odds(p);
            var pools=await RewardPoolsAsync(odds,ct);
            var selected=new List<Item>();
            for(var draw=0;draw<5;draw++)
            {
                var category=SelectCategory(odds,RandomNumberGenerator.GetInt32(100));
                var choices=pools[category];selected.Add(choices[RandomNumberGenerator.GetInt32(choices.Length)]);
            }
            foreach(var group in selected.GroupBy(x=>x.Id))
            {
                var owned=await context.PlayerInventoryItems.SingleOrDefaultAsync(x=>x.PlayerProfileId==player.Id && x.ItemId==group.Key,ct);
                if(owned==null){owned=new(){PlayerProfileId=player.Id,ItemId=group.Key};context.Add(owned);}
                owned.Quantity=checked(owned.Quantity+group.Count());
            }
            stock!.Quantity--;
            var result=new ShopBoxOpenResponse{RequestId=requestId,ProductId=productId,QuantityOwned=stock.Quantity,CoinsAfter=player.Gold,
                Rewards=selected.Select(x=>ItemView(x,context.PlayerInventoryItems.Local.Single(i=>i.PlayerProfileId==player.Id && i.ItemId==x.Id).Quantity,player.EquippedCharacterItemId)).ToList()};
            SaveOperation(player,requestId,"OpenBox",productId,result);return result;
        },ct);
    }
}
