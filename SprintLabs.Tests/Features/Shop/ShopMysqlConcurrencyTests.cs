using Compass.Tests.Features.CurrentCommunityResolution;
using Compass.Tests.Features.Missions;
using FluentAssertions;
using Infrastructure.DataAccess;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Shared.Exceptions;
using Shared.Requests.Shop;
using Shared.Responses.Shop;
using System.Net;
namespace Compass.Tests.Features.Shop;

public class ShopMysqlConcurrencyTests
{
    private static async Task<T> Retry<T>(ShopMysqlDatabase db, Func<ApplicationDbContext, ShopService, Task<T>> action)
    {
        for (int attempt = 0; ; attempt++)
        {
            using var context = db.Context();
            try { return await action(context, new ShopService(context)); }
            catch (GenericException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < 3) { await Task.Delay(50); }
        }
    }
    private static async Task<long> Seed(ShopMysqlDatabase db)
    {
        using var fixture = new MissionFixture(db.Context()); await fixture.SeedAsync();
        fixture.Context.Players.Single(x => x.Id == 101).Gold = 1000;
        await fixture.Context.SaveChangesAsync();
        return (await new ShopService(fixture.Context).SeedMagicalBoxesAsync(40, default))[0].ProductId;
    }

    [MysqlFact]
    public async Task Concurrent_same_request_charges_once_and_returns_one_receipt()
    {
        await using var db=await ShopMysqlDatabase.CreateAsync(); var product=await Seed(db); var request=Guid.NewGuid();
        var results=await Task.WhenAll(Retry(db,(_,s)=>s.PurchaseBoxAsync(1,product,request,default)),Retry(db,(_,s)=>s.PurchaseBoxAsync(1,product,request,default)));
        results.Select(x=>x.RequestId).Distinct().Should().HaveCount(1);
        using var verify=db.Context(); verify.Players.Single(x=>x.Id==101).Gold.Should().Be(900);
        verify.Set<Domain.Models.ShopOperation>().Count().Should().Be(1); verify.Set<Domain.Models.PlayerBox>().Single().Quantity.Should().Be(1);
    }

    [MysqlFact]
    public async Task Concurrent_different_products_cannot_spend_the_same_coins()
    {
        await using var db=await ShopMysqlDatabase.CreateAsync(); var first=await Seed(db);
        using(var c=db.Context()) {c.Players.Single(x=>x.Id==101).Gold=100; await c.SaveChangesAsync();}
        var second=await Retry(db,async (c,s)=>{
            var product=(await s.CatalogueAsync(1,default)).Boxes[1];
            c.ShopProducts.Single(x=>x.Id==product.ProductId).CoinPrice=100;await c.SaveChangesAsync();return product;
        });
        async Task<bool> Purchase(long id)
        {
            try {await Retry(db,(_,s)=>s.PurchaseBoxAsync(1,id,Guid.NewGuid(),default));return true;}
            catch(GenericException e) when(e.StatusCode==HttpStatusCode.Conflict){return false;}
        }
        var result=await Task.WhenAll(Purchase(first),Purchase(second.ProductId)); result.Count(x=>x).Should().Be(1);
        using var verify=db.Context(); verify.Players.Single(x=>x.Id==101).Gold.Should().Be(0);
        verify.Set<Domain.Models.ShopOperation>().Count().Should().Be(1);
    }

    [MysqlFact]
    public async Task Concurrent_mission_claim_and_purchase_preserve_both_wallet_changes()
    {
        await using var db=await ShopMysqlDatabase.CreateAsync(); var product=await Seed(db); long mission;
        using(var fixture=new MissionFixture(db.Context()))
        {
            var m=await fixture.AssignAsync("Boolean",1); mission=m.PlayerMissionId;
            await fixture.Service.EventsAsync(new(){fixture.Event()},default);
        }
        var purchase=Retry(db,(_,s)=>s.PurchaseBoxAsync(1,product,Guid.NewGuid(),default));
        var claim=Retry(db,(c,_)=>new MissionService(c,new Infrastructure.Services.LevelProgressionService()).ClaimAsync(1,mission,default));
        await Task.WhenAll(purchase,claim);
        using var verify=db.Context(); verify.Players.Single(x=>x.Id==101).Gold.Should().Be(920);
        verify.Players.Single(x=>x.Id==101).Experience.Should().Be(50);
    }


    [MysqlFact]
    public async Task Concurrent_open_retries_keep_exactly_one_ordered_five_reward_receipt()
    {
        await using var db=await ShopMysqlDatabase.CreateAsync();var product=await Seed(db);
        await Retry(db,(_,s)=>s.PurchaseBoxAsync(1,product,Guid.NewGuid(),default));var request=Guid.NewGuid();
        var results=await Task.WhenAll(Retry(db,(_,s)=>s.OpenBoxAsync(1,product,request,default)),Retry(db,(_,s)=>s.OpenBoxAsync(1,product,request,default)));
        results.Count(x=>x.IsReplay).Should().Be(1);results[0].Rewards.Select(x=>x.ItemId).Should().Equal(results[1].Rewards.Select(x=>x.ItemId));
        using var verify=db.Context();verify.PlayerInventoryItems.Sum(x=>x.Quantity).Should().Be(5);verify.Set<Domain.Models.PlayerBox>().Single().Quantity.Should().Be(0);
    }

    [MysqlFact]
    public async Task Concurrent_sale_retries_credit_exactly_one_copy()
    {
        await using var db=await ShopMysqlDatabase.CreateAsync();var product=await Seed(db);
        await Retry(db,(_,s)=>s.PurchaseBoxAsync(1,product,Guid.NewGuid(),default));
        var opened=await Retry(db,(_,s)=>s.OpenBoxAsync(1,product,Guid.NewGuid(),default));var itemId=opened.Rewards[0].ItemId;var request=Guid.NewGuid();
        var results=await Task.WhenAll(Retry(db,(_,s)=>s.SellItemAsync(1,itemId,request,default)),Retry(db,(_,s)=>s.SellItemAsync(1,itemId,request,default)));
        results.Count(x=>x.IsReplay).Should().Be(1);
        using var verify=db.Context();verify.PlayerInventoryItems.Sum(x=>x.Quantity).Should().Be(4);verify.Players.Single(x=>x.Id==101).Gold.Should().Be(900+results[0].CoinsGained);
    }

    private sealed class ShopMysqlDatabase : IAsyncDisposable
    {
        private readonly string _connection;
        private ShopMysqlDatabase(string connection) => _connection = connection;
        public ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(_connection, new MySqlServerVersion(new Version(8, 0, 36))).Options);
        public static async Task<ShopMysqlDatabase> CreateAsync()
        {
            var connection = Environment.GetEnvironmentVariable("SPRINTLABS_MYSQL_TEST_CONNECTION")
                ?? throw new InvalidOperationException("A dedicated MySQL test connection is required.");
            var builder = new MySqlConnectionStringBuilder(connection);
            if (!builder.Database.StartsWith("sprintlabs-test", StringComparison.OrdinalIgnoreCase) &&
                !builder.Database.StartsWith("compass-test", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A dedicated test database prefix is required.");
            // Each test creates and removes only this unique database, never the supplied base database.
            builder.Database = "sprintlabs-test-shop-" + Guid.NewGuid().ToString("N");
            var database = new ShopMysqlDatabase(builder.ConnectionString);
            using var context = database.Context(); await context.Database.EnsureCreatedAsync();
            return database;
        }
        public async ValueTask DisposeAsync()
        {
            using var context = Context(); await context.Database.EnsureDeletedAsync();
        }
    }
}
