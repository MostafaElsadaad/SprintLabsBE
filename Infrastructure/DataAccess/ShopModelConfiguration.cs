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
            e.HasIndex(x => x.BoxKey).IsUnique();
            e.Property(x => x.BoxKey).HasMaxLength(100);
            e.Property(x => x.IconKey).HasMaxLength(200);
            e.Property(x => x.CategoryOddsJson).HasColumnType("longtext");
            e.ToTable(t => t.HasCheckConstraint("CK_ShopProducts_DirectPrice", "(`ItemId` IS NULL AND `CoinPrice` IS NULL AND `BoxKey` IS NULL) OR (`ItemId` IS NOT NULL AND `CoinPrice` IS NOT NULL AND `CoinPrice` > 0 AND `BoxKey` IS NULL) OR (`ItemId` IS NULL AND `CoinPrice` IS NOT NULL AND `CoinPrice` > 0 AND `BoxKey` IS NOT NULL)"));
        });
        model.Entity<Item>(e => { e.Property(x => x.Category).HasMaxLength(32); e.ToTable(t => t.HasCheckConstraint("CK_Items_SellPrice", "`SellPrice` >= 0")); });
        model.Entity<PlayerBox>(e =>
        {
            e.ToTable("PlayerBoxes", t => t.HasCheckConstraint("CK_PlayerBoxes_Quantity", "`Quantity` >= 0"));
            e.HasIndex(x => new { x.PlayerProfileId, x.ShopProductId }).IsUnique();
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ShopProduct>().WithMany().HasForeignKey(x => x.ShopProductId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<PlayerCosmeticEquipment>(e =>
        {
            e.ToTable("PlayerCosmeticEquipment"); e.Property(x => x.Category).HasMaxLength(32).IsRequired();
            e.HasIndex(x => new { x.PlayerProfileId, x.Category }).IsUnique();
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<ShopOperation>(e =>
        {
            e.ToTable("ShopOperations");
            e.Property(x => x.RequestId).HasMaxLength(32).IsRequired(); e.Property(x => x.Action).HasMaxLength(32).IsRequired();
            e.Property(x => x.ResultJson).HasColumnType("longtext").IsRequired();
            e.HasIndex(x => new { x.PlayerProfileId, x.RequestId }).IsUnique();
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerProfileId).OnDelete(DeleteBehavior.Restrict);
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
