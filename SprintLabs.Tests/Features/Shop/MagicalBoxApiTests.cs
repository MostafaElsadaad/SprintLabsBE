using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
namespace Compass.Tests.Features.Shop;

public partial class ShopApiTests
{
    private static async Task<JsonElement> SeedBoxes(Fixture f)
    {
        using var admin = Client(f, "40");
        return await Data(await admin.PostAsync("/api/v1/admin/shop/seed/magical-boxes", null));
    }
    private static string BoxBuy(long id) => $"/api/v1/Shop/boxes/{id}/purchase";
    private static string BoxOpen(long id) => $"/api/v1/Shop/me/boxes/{id}/open";
    private static string Sell(long id) => $"/api/v1/Shop/me/items/{id}/sell";


    [Theory]
    [InlineData(0,5,30,25,25,15)]
    [InlineData(1,10,30,20,20,20)]
    [InlineData(2,20,30,15,15,20)]
    public async Task Every_integer_rng_boundary_has_exact_approved_category_weight(int boxIndex,int character,int skin,int pet,int accessory,int effect)
    {
        using var f=new Fixture();await f.SeedAsync();await SeedBoxes(f);using var context=f.Context();
        var box=(await new Infrastructure.Services.ShopService(context).CatalogueAsync(10,default)).Boxes[boxIndex];
        var actual=Enumerable.Range(0,100).Select(roll=>Infrastructure.Services.ShopService.SelectCategory(box.Odds,roll)).ToArray();
        var names=new[]{"Character","Skin","Pet","Accessory","Effect"};var expected=new[]{character,skin,pet,accessory,effect};
        actual.Should().Equal(names.SelectMany((name,index)=>Enumerable.Repeat(name,expected[index])));
    }

    private static async Task<T> ConcurrentRetry<T>(Fixture f,Func<Infrastructure.Services.ShopService,Task<T>> action)
    {
        for(var attempt=0;;attempt++)
        {
            using var db=f.Context();
            try{return await action(new Infrastructure.Services.ShopService(db));}
            catch(Shared.Exceptions.GenericException e) when(e.StatusCode==HttpStatusCode.ServiceUnavailable && attempt<10){await Task.Delay(30);}
        }
    }
    [Fact]
    public async Task Concurrent_sqlite_same_buy_open_and_sale_requests_return_one_saved_result_each()
    {
        using var f=new Fixture(true);await f.SeedAsync();await SeedBoxes(f);
        using var context=f.Context();var box=(await new Infrastructure.Services.ShopService(context).CatalogueAsync(10,default)).Boxes[0].ProductId;
        var buyKey=Guid.NewGuid();
        var buys=await Task.WhenAll(Task.Run(()=>ConcurrentRetry(f,s=>s.PurchaseBoxAsync(10,box,buyKey,default))),Task.Run(()=>ConcurrentRetry(f,s=>s.PurchaseBoxAsync(10,box,buyKey,default))));
        buys.Count(x=>x.IsReplay).Should().Be(1);
        var openKey=Guid.NewGuid();
        var opens=await Task.WhenAll(Task.Run(()=>ConcurrentRetry(f,s=>s.OpenBoxAsync(10,box,openKey,default))),Task.Run(()=>ConcurrentRetry(f,s=>s.OpenBoxAsync(10,box,openKey,default))));
        opens.Count(x=>x.IsReplay).Should().Be(1);
        opens[0].Rewards.Select(x=>x.ItemId).Should().Equal(opens[1].Rewards.Select(x=>x.ItemId));
        var itemId=opens[0].Rewards[0].ItemId;var saleKey=Guid.NewGuid();
        var sales=await Task.WhenAll(Task.Run(()=>ConcurrentRetry(f,s=>s.SellItemAsync(10,itemId,saleKey,default))),Task.Run(()=>ConcurrentRetry(f,s=>s.SellItemAsync(10,itemId,saleKey,default))));
        sales.Count(x=>x.IsReplay).Should().Be(1);
        using var verify=f.Context();
        (await verify.Set<ShopOperation>().CountAsync()).Should().Be(3);
        (await verify.PlayerInventoryItems.SumAsync(x=>x.Quantity)).Should().Be(4);
        (await verify.Players.SingleAsync(x=>x.Id==100)).Gold.Should().Be(900+sales[0].CoinsGained);
    }

    [Fact]
    public async Task Concurrent_sqlite_purchase_sale_and_real_mission_claim_preserve_all_wallet_changes()
    {
        using var f=new Fixture(true);await f.SeedAsync();await SeedBoxes(f);
        long mission;long itemId;long box;
        using(var fixture=new Compass.Tests.Features.Missions.MissionFixture(f.Context()))
        {
            fixture.Context.Users.Add(new(){Id=1,Name="Mission player",UserName="mission@example.com",Email="mission@example.com"});
            fixture.Context.Players.Add(new(){Id=101,UserId=1,Name="Mission player",Email="mission@example.com",Gold=1000});
            itemId=(await fixture.Context.Items.SingleAsync(x=>x.PrefabKey=="skins/emerald")).Id;
            fixture.Context.PlayerInventoryItems.Add(new(){PlayerProfileId=101,ItemId=itemId,Quantity=1});
            await fixture.Context.SaveChangesAsync();
            var assigned=await fixture.AssignAsync("Boolean",1);mission=assigned.PlayerMissionId;
            await fixture.Service.EventsAsync(new(){fixture.Event()},default);
            box=(await new Infrastructure.Services.ShopService(fixture.Context).CatalogueAsync(1,default)).Boxes[0].ProductId;
        }
        async Task Claim()
        {
            for(var attempt=0;;attempt++)
            {
                using var db=f.Context();
                try{await new Infrastructure.Services.MissionService(db,new Infrastructure.Services.LevelProgressionService()).ClaimAsync(1,mission,default);return;}
                catch(Shared.Exceptions.GenericException e)when(e.StatusCode==HttpStatusCode.ServiceUnavailable && attempt<10){await Task.Delay(30);}
            }
        }
        await Task.WhenAll(Task.Run(()=>ConcurrentRetry(f,s=>s.PurchaseBoxAsync(1,box,Guid.NewGuid(),default))),
            Task.Run(()=>ConcurrentRetry(f,s=>s.SellItemAsync(1,itemId,Guid.NewGuid(),default))),Task.Run(Claim));
        using var verify=f.Context();var player=await verify.Players.SingleAsync(x=>x.Id==101);
        player.Gold.Should().Be(940);player.Experience.Should().Be(50);
        (await verify.Set<PlayerBox>().SingleAsync(x=>x.PlayerProfileId==101)).Quantity.Should().Be(1);
        (await verify.PlayerInventoryItems.SingleAsync(x=>x.PlayerProfileId==101)).Quantity.Should().Be(0);
    }

    [Fact]
    public async Task Legacy_receipt_uuid_cannot_be_reused_for_box_actions_and_new_box_uuid_cannot_be_reused_for_direct_purchase()
    {
        using var f=new Fixture();await f.SeedAsync();await SeedBoxes(f);var direct=await Seed(f);using var c=Client(f);
        var box=(await Data(await c.GetAsync("/api/v1/Shop"))).GetProperty("boxes")[0].GetProperty("productId").GetInt64();
        var legacyKey=Guid.NewGuid();await HistoricalPurchase(f,direct,legacyKey);
        (await c.PostAsJsonAsync(BoxBuy(box),new{requestId=legacyKey})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        var newKey=Guid.NewGuid();await Data(await c.PostAsJsonAsync(BoxBuy(box),new{requestId=newKey}));
        (await c.PostAsJsonAsync(Buy(direct),new{requestId=newKey})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Data(await c.PostAsJsonAsync(Buy(direct),new{requestId=legacyKey}))).GetProperty("isReplay").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Concurrent_sqlite_different_open_requests_cannot_consume_one_box_twice()
    {
        using var f=new Fixture(true);await f.SeedAsync();await SeedBoxes(f);
        using var context=f.Context();var service=new Infrastructure.Services.ShopService(context);var box=(await service.CatalogueAsync(10,default)).Boxes[0].ProductId;
        await service.PurchaseBoxAsync(10,box,Guid.NewGuid(),default);
        async Task<bool> Open()
        {
            try{await ConcurrentRetry(f,s=>s.OpenBoxAsync(10,box,Guid.NewGuid(),default));return true;}
            catch(Shared.Exceptions.GenericException e)when(e.StatusCode==HttpStatusCode.Conflict){return false;}
        }
        var result=await Task.WhenAll(Task.Run(Open),Task.Run(Open));result.Count(x=>x).Should().Be(1);
        using var verify=f.Context();(await verify.PlayerInventoryItems.SumAsync(x=>x.Quantity)).Should().Be(5);
    }
    [Fact]
    public async Task Open_and_sale_receipt_insert_failures_roll_back_all_inventory_and_coin_changes()
    {
        using var f=new Fixture();await f.SeedAsync();await SeedBoxes(f);using var c=Client(f);
        var box=(await Data(await c.GetAsync("/api/v1/Shop"))).GetProperty("boxes")[0].GetProperty("productId").GetInt64();
        await Data(await c.PostAsJsonAsync(BoxBuy(box),new{requestId=Guid.NewGuid()}));
        using(var db=f.Context()) await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailOpen BEFORE INSERT ON ShopOperations BEGIN SELECT RAISE(ABORT, 'forced rollback'); END;");
        var key=Guid.NewGuid();(await c.PostAsJsonAsync(BoxOpen(box),new{requestId=key})).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        using(var db=f.Context()) { (await db.Set<PlayerBox>().SingleAsync()).Quantity.Should().Be(1);(await db.PlayerInventoryItems.CountAsync()).Should().Be(0);await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailOpen;"); }
        var opened=await Data(await c.PostAsJsonAsync(BoxOpen(box),new{requestId=key}));var itemId=opened.GetProperty("rewards")[0].GetProperty("itemId").GetInt64();
        using(var db=f.Context()) await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailSale BEFORE INSERT ON ShopOperations BEGIN SELECT RAISE(ABORT, 'forced rollback'); END;");
        var saleKey=Guid.NewGuid();(await c.PostAsJsonAsync(Sell(itemId),new{requestId=saleKey})).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        using(var db=f.Context()) { (await db.PlayerInventoryItems.SumAsync(x=>x.Quantity)).Should().Be(5);(await db.Players.SingleAsync(x=>x.Id==100)).Gold.Should().Be(900);await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailSale;"); }
        (await c.PostAsJsonAsync(Sell(itemId),new{requestId=saleKey})).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Magical_catalogue_has_three_label_only_boxes_and_no_direct_products_and_repeat_seed_preserves_values()
    {
        using var f = new Fixture(); await f.SeedAsync(); await SeedBoxes(f); using var c = Client(f);
        var catalogue = await Data(await c.GetAsync("/api/v1/Shop"));
        catalogue.GetProperty("products").GetArrayLength().Should().Be(0);
        var boxes = catalogue.GetProperty("boxes").EnumerateArray().ToArray();
        boxes.Select(x => x.GetProperty("coinPrice").GetInt32()).Should().Equal(100,200,300);
        boxes.Select(x => x.GetProperty("displayLevel").GetInt32()).Should().Equal(1,5,9);
        boxes.Should().OnlyContain(x => x.GetProperty("drawCount").GetInt32() == 5);
        boxes[0].GetProperty("odds").EnumerateArray().Select(x=>x.GetProperty("percent").GetInt32()).Should().Equal(5,30,25,25,15);
        var galaxy = boxes[2].GetProperty("productId").GetInt64();
        (await Data(await c.PostAsJsonAsync(BoxBuy(galaxy),new {requestId=Guid.NewGuid()}))).GetProperty("coinsAfter").GetInt32().Should().Be(700);
        using(var db=f.Context()){ var p=await db.ShopProducts.SingleAsync(x=>x.Id==galaxy); p.CoinPrice=350;p.IsActive=false;await db.SaveChangesAsync(); }
        await SeedBoxes(f);
        using var verify=f.Context(); (await verify.ShopProducts.SingleAsync(x=>x.Id==galaxy)).CoinPrice.Should().Be(350);
        (await verify.Players.SingleAsync(x=>x.Id==100)).Gold.Should().Be(700);
        (await verify.Items.CountAsync()).Should().Be(14);
    }

    [Fact]
    public async Task Buy_open_replay_and_sell_are_atomic_stacked_ordered_and_use_authenticated_owner()
    {
        using var f=new Fixture();await f.SeedAsync();await SeedBoxes(f);using var c=Client(f);
        var box=(await Data(await c.GetAsync("/api/v1/Shop"))).GetProperty("boxes")[0].GetProperty("productId").GetInt64();
        // Configure a single eligible reward through persisted operator configuration: all five independent draws must stack.
        using(var db=f.Context())
        {
            var p=await db.ShopProducts.SingleAsync(x=>x.Id==box);
            db.Entry(p).Property("CategoryOddsJson").CurrentValue="[{\"Category\":\"Skin\",\"Percent\":100}]";
            var rewards=await db.Items.Where(x=>x.PrefabKey.StartsWith("skins/")).OrderBy(x=>x.Id).ToListAsync();
            foreach(var item in rewards.Skip(1)) db.Entry(item).Property("IsBoxReward").CurrentValue=false;
            await db.SaveChangesAsync();
        }
        var buyKey=Guid.NewGuid();var purchase=await Data(await c.PostAsJsonAsync(BoxBuy(box),new{requestId=buyKey,userId=11}));
        purchase.GetProperty("quantityOwned").GetInt32().Should().Be(1);
        (await Data(await c.PostAsJsonAsync(BoxBuy(box),new{requestId=buyKey}))).GetProperty("isReplay").GetBoolean().Should().BeTrue();
        var openKey=Guid.NewGuid();var opened=await Data(await c.PostAsJsonAsync(BoxOpen(box),new{requestId=openKey}));
        var rewardsJson=opened.GetProperty("rewards");rewardsJson.GetArrayLength().Should().Be(5);
        var itemId=rewardsJson[0].GetProperty("itemId").GetInt64();
        rewardsJson.EnumerateArray().Should().OnlyContain(x=>x.GetProperty("itemId").GetInt64()==itemId);
        opened.GetProperty("quantityOwned").GetInt32().Should().Be(0);
        await Data(await c.PutAsJsonAsync("/api/v1/Shop/me/equipped-item",new{itemId}));
        var saleKey=Guid.NewGuid();var sale=await Data(await c.PostAsJsonAsync(Sell(itemId),new{requestId=saleKey}));
        sale.GetProperty("coinsGained").GetInt32().Should().Be(20);sale.GetProperty("quantityOwned").GetInt32().Should().Be(4);
        (await Data(await c.PostAsJsonAsync(Sell(itemId),new{requestId=saleKey}))).GetProperty("isReplay").GetBoolean().Should().BeTrue();
        var replay=await Data(await c.PostAsJsonAsync(BoxOpen(box),new{requestId=openKey}));
        replay.GetProperty("rewards").GetRawText().Should().Be(rewardsJson.GetRawText());
        for(var n=0;n<3;n++) (await c.PostAsJsonAsync(Sell(itemId),new{requestId=Guid.NewGuid()})).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync(Sell(itemId),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await c.PostAsJsonAsync(BoxOpen(box),new{requestId=buyKey})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var other=Client(f,"11");
        (await other.PostAsJsonAsync(Sell(itemId),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await other.PutAsJsonAsync("/api/v1/Shop/me/equipped-item",new{itemId})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        var inventory=await Data(await c.GetAsync("/api/v1/Shop/me/inventory"));
        inventory.GetProperty("items")[0].GetProperty("quantityOwned").GetInt32().Should().Be(1);
        inventory.GetProperty("equippedCosmetics")[0].GetProperty("itemId").GetInt64().Should().Be(itemId);
        inventory.GetProperty("coins").GetInt32().Should().Be(980);
        await Data(await c.PutAsJsonAsync("/api/v1/Shop/me/equipped-item",new{itemId=(long?)null,category="Skin"}));
        (await c.PostAsJsonAsync(Sell(itemId),new{requestId=Guid.NewGuid()})).EnsureSuccessStatusCode();
        (await Data(await c.GetAsync("/api/v1/Shop/me/inventory"))).GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Box_receipt_failure_rolls_back_and_missing_pool_never_consumes_box()
    {
        using var f=new Fixture();await f.SeedAsync();await SeedBoxes(f);using var c=Client(f);
        var box=(await Data(await c.GetAsync("/api/v1/Shop"))).GetProperty("boxes")[0].GetProperty("productId").GetInt64();
        var key=Guid.NewGuid();
        using(var db=f.Context()) await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailBox BEFORE INSERT ON ShopOperations BEGIN SELECT RAISE(ABORT, 'forced rollback'); END;");
        (await c.PostAsJsonAsync(BoxBuy(box),new{requestId=key})).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        using(var db=f.Context()) { (await db.Players.SingleAsync(x=>x.Id==100)).Gold.Should().Be(1000);await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailBox;"); }
        (await c.PostAsJsonAsync(BoxBuy(box),new{requestId=key})).EnsureSuccessStatusCode();
        using(var db=f.Context()) { foreach(var i in await db.Items.ToListAsync()) db.Entry(i).Property("IsBoxReward").CurrentValue=false;await db.SaveChangesAsync(); }
        (await c.PostAsJsonAsync(BoxOpen(box),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        var inventory=await Data(await c.GetAsync("/api/v1/Shop/me/inventory"));
        inventory.GetProperty("boxes")[0].GetProperty("quantityOwned").GetInt32().Should().Be(1);
        inventory.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Box_purchase_rejects_missing_pool_without_charging_and_open_failure_rolls_back_rewards()
    {
        using var f=new Fixture();await f.SeedAsync();await SeedBoxes(f);using var c=Client(f);
        var box=(await Data(await c.GetAsync("/api/v1/Shop"))).GetProperty("boxes")[0].GetProperty("productId").GetInt64();
        using(var db=f.Context()) { foreach(var i in await db.Items.ToListAsync()) db.Entry(i).Property("IsBoxReward").CurrentValue=false;await db.SaveChangesAsync(); }
        (await c.PostAsJsonAsync(BoxBuy(box),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var verify=f.Context();(await verify.Players.SingleAsync(x=>x.Id==100)).Gold.Should().Be(1000);
    }

    [Theory]
    [InlineData("quantum-scholar", "characters/quantum-scholar")]
    [InlineData("king-of-knowledge", "characters/king-of-knowledge")]
    public async Task Default_character_cannot_be_sold_even_if_legacy_inventory_contains_it(string key, string prefab)
    {
        using var f=new Fixture();await f.SeedAsync();using var c=Client(f);
        using(var db=f.Context()) {db.Items.Add(new(){Id=998,ItemKey=key,Name="Quantum Scholar",PrefabKey=prefab,IsCharacter=true});db.PlayerInventoryItems.Add(new(){PlayerProfileId=100,ItemId=998,Quantity=1});await db.SaveChangesAsync();}
        (await c.PostAsJsonAsync(Sell(998),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reused_service_reloads_inventory_after_an_interleaved_reward_before_selling()
    {
        using var f=new Fixture();await f.SeedAsync();await SeedBoxes(f);
        using(var db=f.Context()) {var item=await db.Items.SingleAsync(x=>x.PrefabKey=="skins/emerald");db.PlayerInventoryItems.Add(new(){PlayerProfileId=100,ItemId=item.Id,Quantity=3});await db.SaveChangesAsync();}
        using var edit=f.Context();var service=new Infrastructure.Services.ShopService(edit);
        var itemId=(await edit.Items.SingleAsync(x=>x.PrefabKey=="skins/emerald")).Id;
        await service.SellItemAsync(10,itemId,Guid.NewGuid(),default);
        using(var reward=f.Context()) await reward.PlayerInventoryItems.Where(x=>x.PlayerProfileId==100 && x.ItemId==itemId).ExecuteUpdateAsync(x=>x.SetProperty(i=>i.Quantity,7));
        var result=await service.SellItemAsync(10,itemId,Guid.NewGuid(),default);result.QuantityOwned.Should().Be(6);
    }

    [Fact]
    public async Task Magical_routes_require_auth_admin_uuid_and_funds()
    {
        using var f=new Fixture();await f.SeedAsync();await SeedBoxes(f);using var c=Client(f);using var anon=Client(f,null);using var poor=Client(f,"11");
        var box=(await Data(await c.GetAsync("/api/v1/Shop"))).GetProperty("boxes")[0].GetProperty("productId").GetInt64();
        (await anon.PostAsJsonAsync(BoxBuy(box),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await c.PostAsync("/api/v1/admin/shop/seed/magical-boxes",null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await poor.PostAsJsonAsync(BoxBuy(box),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await c.PostAsJsonAsync(BoxBuy(box),new{requestId=Guid.Empty})).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await c.PostAsJsonAsync(BoxOpen(box),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await c.PutAsJsonAsync("/api/v1/Shop/me/equipped-item",new{itemId=(long?)null})).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
