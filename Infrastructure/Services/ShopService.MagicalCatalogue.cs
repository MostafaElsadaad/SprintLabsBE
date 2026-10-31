using System.Text.Json;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Responses.Shop;
namespace Infrastructure.Services;

public partial class ShopService
{
    private static readonly (string Category,string Key,string Name,int Price)[] MagicalItems =
    {
        ("Character","characters/tech-smith","Tech Smith",50), ("Character","characters/soul-mendrix","Soul Mendrix",50),
        ("Skin","skins/emerald","Emerald Skin",20), ("Skin","skins/rose","Rose Skin",20), ("Skin","skins/royal","Royal Skin",20),
        ("Pet","pets/frost","Frost Pet",15), ("Pet","pets/amber","Amber Pet",15), ("Pet","pets/moon","Moon Pet",15),
        ("Accessory","accessories/crystal-crown","Crystal Crown",10), ("Accessory","accessories/amethyst-crown","Amethyst Crown",10), ("Accessory","accessories/royal-crown","Royal Crown",10),
        ("Effect","effects/frost","Frost Effect",10), ("Effect","effects/golden","Golden Effect",10), ("Effect","effects/void","Void Effect",10)
    };
    private static bool BundledReward(Item item) => MagicalItems.Any(x=>x.Category==CategoryOf(item) && x.Key==item.PrefabKey);
    public async Task<List<ShopBoxResponse>> SeedMagicalBoxesAsync(long adminUserId,CancellationToken ct)
    {
        await AdminAsync(adminUserId,ct);
        try
        {
            await using var tx=await context.Database.BeginTransactionAsync(ct);
            foreach(var definition in MagicalItems)
            {
                var key=definition.Category=="Character" ? definition.Key.Split('/')[1] : definition.Key.Replace('/','-');
                var item=await context.Items.SingleOrDefaultAsync(x=>x.ItemKey==key,ct);
                if(item==null)context.Items.Add(new Item{ItemKey=key,Name=definition.Name,Category=definition.Category,IsCharacter=definition.Category=="Character",
                    SellPrice=definition.Price,IsBoxReward=true,IconKey=definition.Key,PrefabKey=definition.Key});
                else if(item.Category==null && item.IsCharacter && definition.Category=="Character")
                { // Enrich pre-feature characters once; retain every existing visual/name and operator setting.
                    item.Category="Character";item.SellPrice=definition.Price;item.IsBoxReward=true;
                }
            }
            var definitions=new[] {("normal-box","Normal Box","boxes/normal",1,100,new[]{5,30,25,25,15}),
                ("crystal-box","Crystal Box","boxes/crystal",5,200,new[]{10,30,20,20,20}),
                ("galaxy-box","Galaxy Box","boxes/galaxy",9,300,new[]{20,30,15,15,20})};
            foreach(var d in definitions)
                if(!await context.ShopProducts.AnyAsync(x=>x.BoxKey==d.Item1,ct))context.ShopProducts.Add(new ShopProduct{BoxKey=d.Item1,Name=d.Item2,
                    IconKey=d.Item3,DisplayLevel=d.Item4,CoinPrice=d.Item5,CategoryOddsJson=JsonSerializer.Serialize(Categories.Select((c,i)=>new ShopCategoryOddsResponse{Category=c,Percent=d.Item6[i]}))});
            await context.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return (await context.ShopProducts.AsNoTracking().Where(x=>x.BoxKey!=null).OrderBy(x=>x.Id).ToListAsync(ct)).Select(x=>BoxView(x,0)).ToList();
        }
        catch(DbUpdateException){context.ChangeTracker.Clear();throw Unavailable();}
        catch(System.Data.Common.DbException){context.ChangeTracker.Clear();throw Unavailable();}
    }
}
