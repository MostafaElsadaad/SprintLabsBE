using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using API.Controllers;
using API.Exceptions;
using Application;
using Asp.Versioning;
using Domain.Models;
using Domain.Repositories;
using Domain.Services;
using FluentAssertions;
using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Responses;

namespace Compass.Tests.Features.Shop;

public class ShopApiTests
{
    private static HttpClient Client(Fixture f, string? user = "10")
    {
        var c = f.Server.CreateClient(); if (user != null) c.DefaultRequestHeaders.Add("X-Test-User", user); return c;
    }
    private static async Task<JsonElement> Data(HttpResponseMessage r)
    {
        if (!r.IsSuccessStatusCode) throw new InvalidOperationException(await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
    }
    private static async Task<long> Seed(Fixture f)
    {
        using var admin = Client(f,"40");
        return (await Data(await admin.PostAsync("/api/v1/admin/shop/seed/example", null))).GetProperty("productId").GetInt64();
    }
    private static string Buy(long id) => $"/api/v1/Shop/products/{id}/purchase";
    private static object Product(int price = 500, bool active = true) => new { name = "King of Knowledge", coinPrice = price, iconKey = "shop/king", prefabKey = "characters/king", isActive = active };

    [Fact]
    public async Task Catalogue_purchase_inventory_and_use_roundtrip_keep_wallet_and_identity_authoritative()
    {
        using var f = new Fixture(); await f.SeedAsync(); var id = await Seed(f); using var c = Client(f);
        var catalog = await Data(await c.GetAsync("/api/v1/Shop"));
        catalog.GetProperty("coins").GetInt32().Should().Be(1000);
        catalog.GetProperty("products").GetArrayLength().Should().Be(1);
        var key = Guid.NewGuid();
        var receipt = await Data(await c.PostAsJsonAsync(Buy(id),new { requestId = key, coins = 0, price = 1, userId = 11, playerProfileId = 200 }));
        receipt.GetProperty("coinsSpent").GetInt32().Should().Be(500);
        receipt.GetProperty("coinsAfter").GetInt32().Should().Be(500);
        receipt.GetProperty("isReplay").GetBoolean().Should().BeFalse();
        var itemId = receipt.GetProperty("itemId").GetInt64();
        var inventory = await Data(await c.GetAsync("/api/v1/Shop/me/inventory"));
        inventory.GetProperty("items").GetArrayLength().Should().Be(1);
        inventory.GetProperty("equippedCharacterItemId").ValueKind.Should().Be(JsonValueKind.Null);
        var selected = await Data(await c.PutAsJsonAsync("/api/v1/Shop/me/equipped-character",new { itemId }));
        selected.GetProperty("equippedCharacterItemId").GetInt64().Should().Be(itemId);
        selected.GetProperty("items")[0].GetProperty("isEquipped").GetBoolean().Should().BeTrue();
        using var anotherRequest = Client(f);
        (await Data(await anotherRequest.GetAsync("/api/v1/Shop/me/inventory"))).GetProperty("equippedCharacterItemId").GetInt64().Should().Be(itemId);
        (await Data(await c.PutAsJsonAsync("/api/v1/Shop/me/equipped-character",new { itemId = (long?)null }))).GetProperty("equippedCharacterItemId").ValueKind.Should().Be(JsonValueKind.Null);
        using var db = f.Context();
        var player = await db.Players.SingleAsync(x => x.Id == 100); player.Gold.Should().Be(500); player.Experience.Should().Be(50);
        (await db.Players.SingleAsync(x => x.Id == 200)).Gold.Should().Be(0);
        (await db.Set<ShopPurchase>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Retry_returns_same_receipt_even_after_price_change_and_deactivation_without_double_charge()
    {
        using var f = new Fixture(); await f.SeedAsync(); var id=await Seed(f); using var c=Client(f); var key=Guid.NewGuid();
        var first=await Data(await c.PostAsJsonAsync(Buy(id),new { requestId=key }));
        using var admin=Client(f,"40");
        await Data(await admin.PutAsJsonAsync("/api/v1/admin/shop/products/king-of-knowledge",Product(800,false)));
        var repeat=await Data(await c.PostAsJsonAsync(Buy(id),new { requestId=key }));
        repeat.GetProperty("purchaseId").GetInt64().Should().Be(first.GetProperty("purchaseId").GetInt64());
        repeat.GetProperty("coinsSpent").GetInt32().Should().Be(500);
        repeat.GetProperty("isReplay").GetBoolean().Should().BeTrue();
        using var db=f.Context(); (await db.Players.SingleAsync(x=>x.Id==100)).Gold.Should().Be(500);
        (await db.PlayerInventoryItems.SingleAsync()).Quantity.Should().Be(1);
    }

    [Fact]
    public async Task Already_owned_with_another_request_or_from_a_mission_is_not_charged()
    {
        using var f = new Fixture(); await f.SeedAsync(); var id=await Seed(f); using var c=Client(f);
        (await c.PostAsJsonAsync(Buy(id),new { requestId=Guid.NewGuid() })).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync(Buy(id),new { requestId=Guid.NewGuid() })).StatusCode.Should().Be(HttpStatusCode.Conflict);
        using(var db=f.Context()) { db.PlayerInventoryItems.Add(new(){PlayerProfileId=200,ItemId=(await db.ShopProducts.SingleAsync()).ItemId!.Value,Quantity=2}); await db.SaveChangesAsync(); }
        using var other=Client(f,"11");
        (await other.PostAsJsonAsync(Buy(id),new { requestId=Guid.NewGuid() })).StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var verify=f.Context(); (await verify.Set<ShopPurchase>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Insufficient_coins_and_inactive_or_unknown_products_never_create_ownership()
    {
        using var f = new Fixture(); await f.SeedAsync(); var id=await Seed(f); using var poor=Client(f,"11");
        (await poor.PostAsJsonAsync(Buy(id),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var c=Client(f); using var admin=Client(f,"40");
        await Data(await admin.PutAsJsonAsync("/api/v1/admin/shop/products/king-of-knowledge",Product(active:false)));
        (await c.PostAsJsonAsync(Buy(id),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await c.PostAsJsonAsync(Buy(9999),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Data(await c.GetAsync("/api/v1/Shop"))).GetProperty("products").GetArrayLength().Should().Be(0);
        using var db=f.Context(); (await db.PlayerInventoryItems.CountAsync()).Should().Be(0); (await db.Set<ShopPurchase>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Request_key_cannot_be_reused_for_a_different_product_and_history_is_per_player()
    {
        using var f=new Fixture(); await f.SeedAsync(); var id=await Seed(f); using var c=Client(f); using var admin=Client(f,"40");
        var second=await Data(await admin.PutAsJsonAsync("/api/v1/admin/shop/products/another-character",Product(100)));
        var key=Guid.NewGuid(); await Data(await c.PostAsJsonAsync(Buy(id),new{requestId=key}));
        (await c.PostAsJsonAsync(Buy(second.GetProperty("productId").GetInt64()),new{requestId=key})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Data(await c.GetAsync("/api/v1/Shop/me/purchases?page=1&pageSize=1"))).GetArrayLength().Should().Be(1);
        using var other=Client(f,"11"); (await Data(await other.GetAsync("/api/v1/Shop/me/purchases"))).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Equip_rejects_unowned_and_non_character_inventory_without_charging()
    {
        using var f=new Fixture(); await f.SeedAsync(); var id=await Seed(f); using var c=Client(f);
        using(var db=f.Context())
        {
            db.Items.Add(new Item{Id=500,ItemKey="mission-reward",Name="Reward"}); await db.SaveChangesAsync();
            db.PlayerInventoryItems.Add(new(){PlayerProfileId=100,ItemId=500,Quantity=1}); await db.SaveChangesAsync();
        }
        (await c.PutAsJsonAsync("/api/v1/Shop/me/equipped-character",new{itemId=500})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await c.PutAsJsonAsync("/api/v1/Shop/me/equipped-character",new{itemId=9999})).StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var db2=f.Context(); (await db2.Players.SingleAsync(x=>x.Id==100)).Gold.Should().Be(1000);
    }

    [Fact]
    public async Task Failed_receipt_insert_rolls_back_wallet_and_ownership_and_allows_safe_retry()
    {
        using var f=new Fixture(); await f.SeedAsync(); var id=await Seed(f); using var c=Client(f); var key=Guid.NewGuid();
        using(var db=f.Context()) await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailPurchase BEFORE INSERT ON ShopPurchases BEGIN SELECT RAISE(ABORT, 'forced rollback'); END;");
        (await c.PostAsJsonAsync(Buy(id),new{requestId=key})).StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        using(var db=f.Context())
        {
            (await db.Players.SingleAsync(x=>x.Id==100)).Gold.Should().Be(1000);
            (await db.PlayerInventoryItems.CountAsync()).Should().Be(0);
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailPurchase;");
        }
        (await c.PostAsJsonAsync(Buy(id),new{requestId=key})).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Old_mission_boxes_remain_unchanged_and_example_seed_never_resets_admin_prices()
    {
        using var f=new Fixture(); await f.SeedAsync();
        using(var db=f.Context()){db.ShopProducts.Add(new ShopProduct{Name="Legacy box"});await db.SaveChangesAsync();}
        var id=await Seed(f); using var admin=Client(f,"40");
        await Data(await admin.PutAsJsonAsync("/api/v1/admin/shop/products/king-of-knowledge",Product(250,false)));
        (await Seed(f)).Should().Be(id);
        using var verify=f.Context(); (await verify.ShopProducts.CountAsync()).Should().Be(2);
        var box=await verify.ShopProducts.SingleAsync(x=>x.Name=="Legacy box"); box.ItemId.Should().BeNull(); box.CoinPrice.Should().BeNull();
        (await verify.ShopProducts.SingleAsync(x=>x.Id==id)).CoinPrice.Should().Be(250);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"requestId\":\"invalid\"}")]
    public async Task Invalid_purchase_request_is_rejected(string json)
    {
        using var f=new Fixture(); await f.SeedAsync(); var id=await Seed(f); using var c=Client(f);
        (await c.PostAsync(Buy(id),new StringContent(json,System.Text.Encoding.UTF8,"application/json"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(-1)] [InlineData(0)]
    public async Task Admin_rejects_nonpositive_prices(int price)
    {
        using var f=new Fixture(); await f.SeedAsync(); using var c=Client(f,"40");
        (await c.PutAsJsonAsync("/api/v1/admin/shop/products/king",Product(price))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Anonymous_nonadmins_suspended_and_unlinked_accounts_cannot_write()
    {
        using var f=new Fixture(); await f.SeedAsync(); var id=await Seed(f); using var anonymous=Client(f,null); using var c=Client(f);
        (await anonymous.GetAsync("/api/v1/Shop")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.PostAsJsonAsync(Buy(id),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await c.PostAsync("/api/v1/admin/shop/seed/example",null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await c.PutAsJsonAsync("/api/v1/admin/shop/products/king",Product())).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using(var db=f.Context()){var user=await db.Users.SingleAsync(x=>x.Id==10);user.Status=Domain.Enums.UserStatus.Suspended;await db.SaveChangesAsync();}
        (await c.PostAsJsonAsync(Buy(id),new{requestId=Guid.NewGuid()})).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var missing=Client(f,"999"); (await missing.GetAsync("/api/v1/Shop/me/inventory")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Purchase_refreshes_a_stale_profile_before_charging_so_a_recent_reward_is_preserved()
    {
        using var f=new Fixture(); await f.SeedAsync(); var product=await Seed(f);
        using var editing=f.Context(); var service=new Infrastructure.Services.ShopService(editing);
        await service.CatalogueAsync(10,default); // Track the old 1000-coin profile.
        using(var award=f.Context()) await award.Players.Where(x=>x.Id==100).ExecuteUpdateAsync(x=>x.SetProperty(p=>p.Gold,1020));
        var receipt=await service.PurchaseAsync(10,product,Guid.NewGuid(),default);
        receipt.CoinsAfter.Should().Be(520);
    }

    [Theory]
    [InlineData("/api/v1/Shop/me/purchases?page=0")]
    [InlineData("/api/v1/Shop/me/purchases?pageSize=101")]
    public async Task Invalid_history_pagination_returns_bad_request(string route)
    {
        using var f=new Fixture(); await f.SeedAsync(); using var c=Client(f);
        (await c.GetAsync(route)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Missing_equipped_item_field_does_not_reset_selection_and_retired_owned_items_remain_usable()
    {
        using var f=new Fixture(); await f.SeedAsync(); var product=await Seed(f); using var c=Client(f);
        var receipt=await Data(await c.PostAsJsonAsync(Buy(product),new{requestId=Guid.NewGuid()}));
        var itemId=receipt.GetProperty("itemId").GetInt64();
        using var admin=Client(f,"40"); await Data(await admin.PutAsJsonAsync("/api/v1/admin/shop/products/king-of-knowledge",Product(active:false)));
        await Data(await c.PutAsJsonAsync("/api/v1/Shop/me/equipped-character",new{itemId}));
        (await c.PutAsJsonAsync("/api/v1/Shop/me/equipped-character",new{})).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Data(await c.GetAsync("/api/v1/Shop/me/inventory"))).GetProperty("equippedCharacterItemId").GetInt64().Should().Be(itemId);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public TestServer Server { get; }
        public Fixture()
        {
            _connection.Open();
            var users = new Mock<IUserService>();
            users.Setup(x => x.GetCurrentUser(It.IsAny<long>())).ReturnsAsync((long id) => new UserIdentityResponse { Id = id, IsSuspended = id == 12 });
            Server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
            {
                services.AddLogging(x => x.ClearProviders());
                services.AddControllers().AddApplicationPart(typeof(PlayerProfilesController).Assembly);
                services.AddApiVersioning(x => { x.DefaultApiVersion = new ApiVersion(1, 0); x.ApiVersionReader = new UrlSegmentApiVersionReader(); });
                services.AddApplicationServices(new ConfigurationBuilder().Build());
                services.AddScoped<ApplicationDbContext>(_ => Context());
                services.AddScoped<IPlayerRepository, PlayerRepository>();
                services.AddScoped<IShopService, Infrastructure.Services.ShopService>();
                services.AddSingleton(users.Object);
                services.AddAuthentication("TestPlayer").AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("TestPlayer", _ => { });
                services.AddAuthorization();
            }).Configure(app =>
            {
                app.UseMiddleware<GlobalExceptionHandler>(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseEndpoints(x => x.MapControllers());
            }));
        }
        public ApplicationDbContext Context() => new SqliteProfileContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        public async Task SeedAsync()
        {
            using var context = Context(); await context.Database.EnsureCreatedAsync();
            context.Users.AddRange(new User { Id = 10, Name = "Current", UserName = "current@example.com", Email = "current@example.com" }, new User { Id = 11, Name = "Other", UserName = "other@example.com", Email = "other@example.com" });
            context.Players.AddRange(
                new Player { Id = 100, UserId = 10, Name = "Current", Email = "current@example.com", AvatarUrl = "legacy.png", Age = 9, Gold = 1000, Experience = 50 },
                new Player { Id = 200, UserId = 11, Name = "Other", Email = "other@example.com" });
            context.Users.Add(new User { Id = 40, Name = "Admin", UserName = "admin@example.com", Email = "admin@example.com", IsPlatformAdmin = true });
            await context.SaveChangesAsync();
        }
        public void Dispose() { Server.Dispose(); _connection.Dispose(); }
    }

    private sealed class SqliteProfileContext : ApplicationDbContext
    {
        public SqliteProfileContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetProperties()))
                if (property.GetDefaultValueSql() == "CURRENT_TIMESTAMP(6)") property.SetDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!long.TryParse(Request.Headers["X-Test-User"], out var id)) return Task.FromResult(AuthenticateResult.NoResult());
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", id.ToString()) }, Scheme.Name)), Scheme.Name)));
        }
    }
}
