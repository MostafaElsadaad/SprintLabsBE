using Domain.Models;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.DataAccess;
internal static class MissionModelConfiguration
{
    public static void Configure(ModelBuilder b)
    {
        b.Entity<MissionTemplate>(e => {
            e.HasIndex(x => x.MissionKey).IsUnique(); e.Property(x => x.MissionKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired(); e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.ProgressField).HasMaxLength(64); e.Property(x => x.ConditionsJson).HasColumnType("longtext");
            e.HasMany(x => x.Rewards).WithOne().HasForeignKey(x => x.MissionTemplateId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<MissionReward>().HasOne<ShopProduct>().WithMany().HasForeignKey(x => x.ShopProductId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MissionActivation>(e => {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired(); e.HasIndex(x => x.Name).IsUnique(); e.HasIndex(x => new { x.Status, x.EndsAt });
            e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.MissionActivationId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<MissionActivationItem>(e => {
            e.HasIndex(x => new { x.MissionActivationId, x.MissionTemplateId }).IsUnique();
            e.HasOne<MissionTemplate>().WithMany().HasForeignKey(x => x.MissionTemplateId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<PlayerMissionAssignment>(e => {
            e.HasIndex(x => new { x.PlayerProfileId, x.MissionActivationId }).IsUnique();
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MissionActivation>().WithMany().HasForeignKey(x => x.MissionActivationId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<PlayerMission>(e => {
            e.HasIndex(x => new { x.PlayerProfileId, x.MissionActivationId, x.MissionTemplateId }).IsUnique();
            e.HasIndex(x => new { x.MissionActivationId, x.Status });
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MissionActivation>().WithMany().HasForeignKey(x => x.MissionActivationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MissionTemplate>().WithMany().HasForeignKey(x => x.MissionTemplateId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.DefinitionSnapshotJson).HasColumnType("longtext");
            e.Property(x => x.ProgressStateJson).HasColumnType("longtext");
            e.Property(x => x.ClaimSnapshotJson).HasColumnType("longtext");
        });
        b.Entity<MissionEventLog>(e => {
            e.HasIndex(x => new { x.PlayerProfileId, x.EventId }).IsUnique(); e.Property(x => x.EventId).HasMaxLength(100).IsRequired();
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Community>().WithMany().HasForeignKey(x => x.CommunityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Match>().WithMany().HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.EventDataJson).HasColumnType("longtext"); e.Property(x => x.ResultJson).HasColumnType("longtext");
        });
        b.Entity<MissionClaimLog>(e => {
            e.HasOne<PlayerMission>().WithMany().HasForeignKey(x => x.PlayerMissionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MissionTemplate>().WithMany().HasForeignKey(x => x.MissionTemplateId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Item>().WithMany().HasForeignKey(x => x.RewardItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ShopProduct>().WithMany().HasForeignKey(x => x.ShopProductId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ShopProduct>(e => {
            e.Property(x => x.Name).HasMaxLength(200); e.HasMany(x => x.BoxRewards).WithOne().HasForeignKey(x => x.ShopProductId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Item>(e => { e.HasIndex(x => x.ItemKey).IsUnique(); e.Property(x => x.ItemKey).HasMaxLength(100);
            e.Property(x => x.Name).HasMaxLength(200); e.Property(x => x.Rarity).HasMaxLength(32);
            e.Property(x => x.IconKey).HasMaxLength(200); e.Property(x => x.PrefabKey).HasMaxLength(200); });
        b.Entity<BoxRewardEntry>().HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<PlayerInventoryItem>(e => {
            e.HasIndex(x => new { x.PlayerProfileId, x.ItemId }).IsUnique();
            e.HasOne<Player>().WithMany().HasForeignKey(x => x.PlayerProfileId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
