using Domain.Models;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.DataAccess;

internal static class ShopModelConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<ShopProduct>(e =>
        {
            e.HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ItemId).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_ShopProducts_DirectPrice", "(`ItemId` IS NULL AND `CoinPrice` IS NULL) OR (`ItemId` IS NOT NULL AND `CoinPrice` IS NOT NULL AND `CoinPrice` > 0)"));
        });
        model.Entity<Player>().HasOne<Item>().WithMany().HasForeignKey(x => x.EquippedCharacterItemId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ShopPurchase>(e =>
        {
            e.ToTable("ShopPurchases", t => t.HasCheckConstraint("CK_ShopPurchases_Coins", "`CoinsSpent` > 0 AND `CoinsAfter` >= 0"));
            e.Property(x => x.RequestId).HasMaxLength(32).IsRequired();
            e.HasIndex(x => new { x.PlayerProfileId, x.RequestId }).IsUnique();
            e.HasIndex(x => new { x.PlayerProfileId, x.ItemId }).IsUnique();
            e.HasIndex(x => new { x.PlayerProfileId, x.Id });
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ShopProduct>().WithMany().HasForeignKey(x => x.ShopProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
