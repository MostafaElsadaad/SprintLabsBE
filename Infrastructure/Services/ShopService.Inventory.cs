using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Responses.Shop;
namespace Infrastructure.Services;

public partial class ShopService
{
    public async Task<ShopItemSaleResponse> SellItemAsync(long userId,long itemId,Guid requestId,CancellationToken ct)
    {
        if(itemId<=0 || requestId==Guid.Empty)throw Invalid();
        return await AtomicAsync(userId,async player=>
        {
            var replay=await ReplayAsync<ShopItemSaleResponse>(player,requestId,"SellItem",itemId,ct);
            if(replay!=null){replay.IsReplay=true;return replay;}
            var owned=await context.PlayerInventoryItems.SingleOrDefaultAsync(x=>x.PlayerProfileId==player.Id && x.ItemId==itemId,ct);
            if(owned?.Quantity is not >0)throw Conflict("Item is not owned.");
            var item=await context.Items.AsNoTracking().SingleAsync(x=>x.Id==itemId,ct);
            var price=SellPriceOf(item);
            if(!Categories.Contains(CategoryOf(item)) || price<=0)throw Conflict("Item cannot be sold.");
            var equipped=player.EquippedCharacterItemId==itemId || await context.Set<PlayerCosmeticEquipment>().AnyAsync(x=>x.PlayerProfileId==player.Id && x.ItemId==itemId,ct);
            if(equipped && owned.Quantity==1)throw Conflict("Last equipped copy cannot be sold.");
            owned.Quantity--;player.Gold=checked(player.Gold+price);
            var result=new ShopItemSaleResponse{RequestId=requestId,ItemId=itemId,CoinsGained=price,CoinsAfter=player.Gold,QuantityOwned=owned.Quantity};
            SaveOperation(player,requestId,"SellItem",itemId,result);return result;
        },ct);
    }
    public async Task<ShopInventoryResponse> EquipItemAsync(long userId,long? itemId,string? category,CancellationToken ct)
    {
        if(itemId is <=0 || itemId==null && !Categories.Contains(category))throw Invalid();
        return await AtomicAsync(userId,async player=>
        {
            if(itemId.HasValue)
            {
                var item=await (from owned in context.PlayerInventoryItems join i in context.Items on owned.ItemId equals i.Id
                    where owned.PlayerProfileId==player.Id && owned.ItemId==itemId && owned.Quantity>0 select i).AsNoTracking().SingleOrDefaultAsync(ct);
                if(item==null)throw Conflict("Item is not owned.");
                category=CategoryOf(item);
                if(!Categories.Contains(category))throw Conflict("Item cannot be equipped.");
                if(category!="Character" && !BundledReward(item))throw Conflict("Item visual unavailable.");
            }
            if(category=="Character")player.EquippedCharacterItemId=itemId;
            else
            {
                var slot=await context.Set<PlayerCosmeticEquipment>().SingleOrDefaultAsync(x=>x.PlayerProfileId==player.Id && x.Category==category,ct);
                if(itemId==null){if(slot!=null)context.Remove(slot);}
                else if(slot==null)context.Add(new PlayerCosmeticEquipment{PlayerProfileId=player.Id,Category=category!,ItemId=itemId.Value});
                else slot.ItemId=itemId.Value;
            }
            player.UpdatedAt=DateTime.UtcNow;await context.SaveChangesAsync(ct);
            return await InventoryForAsync(player,ct);
        },ct);
    }
}
