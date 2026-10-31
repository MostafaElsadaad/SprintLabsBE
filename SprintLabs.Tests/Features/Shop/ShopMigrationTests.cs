using FluentAssertions;
using Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
namespace Compass.Tests.Features.Shop;

public class ShopMigrationTests
{
    [Fact]
    public void Magical_migration_is_additive_preserves_old_receipt_indexes_and_contains_no_player_or_catalogue_data_writes()
    {
        var type=typeof(ApplicationDbContext).Assembly.GetTypes().Single(x=>x.Name=="AddMagicalBoxes");
        var migration=(Migration)Activator.CreateInstance(type)!;var operations=migration.UpOperations;
        operations.OfType<CreateTableOperation>().Select(x=>x.Name).Should().BeEquivalentTo("PlayerBoxes","PlayerCosmeticEquipment","ShopOperations");
        operations.Should().NotContain(x=>x is DropTableOperation || x is DropColumnOperation || x is UpdateDataOperation || x is DeleteDataOperation || x is InsertDataOperation);
        operations.OfType<DropIndexOperation>().Should().BeEmpty();
        operations.OfType<CreateIndexOperation>().Should().Contain(x=>x.Table=="ShopOperations" && x.IsUnique && x.Columns.SequenceEqual(new[]{"PlayerProfileId","RequestId"}));
        var old=migration.TargetModel.FindEntityType("Domain.Models.ShopPurchase")!;
        old.GetIndexes().Should().Contain(x=>x.IsUnique && x.Properties.Select(p=>p.Name).SequenceEqual(new[]{"PlayerProfileId","ItemId"}));
        migration.TargetModel.FindEntityType("Domain.Models.BoxRewardEntry").Should().NotBeNull();
    }

    [Fact]
    public void Migration_preserves_existing_mission_boxes_and_adds_unique_receipts_without_seeding_wallets()
    {
        var type=typeof(ApplicationDbContext).Assembly.GetTypes().Single(x=>x.Name=="AddCoinShop");
        var migration=(Migration)Activator.CreateInstance(type)!;
        // The latest migration target must include the earlier B2C migration after branch integration.
        var player = migration.TargetModel.FindEntityType("Domain.Models.Player")!;
        player.FindProperty("Curriculum").Should().NotBeNull();
        player.FindProperty("Language").Should().NotBeNull();
        migration.TargetModel.FindEntityType("Domain.Models.PlayerEducationOption").Should().NotBeNull();
        var operations=migration.UpOperations;
        operations.OfType<CreateTableOperation>().Single().Name.Should().Be("ShopPurchases");
        operations.OfType<AddColumnOperation>().Where(x=>x.Table=="ShopProducts").Should().OnlyContain(x=>x.IsNullable);
        operations.OfType<AddColumnOperation>().Single(x=>x.Name=="EquippedCharacterItemId").IsNullable.Should().BeTrue();
        operations.OfType<AddColumnOperation>().Single(x=>x.Name=="IsCharacter").DefaultValue.Should().Be(false);
        operations.OfType<CreateIndexOperation>().Should().Contain(x=>x.Table=="ShopPurchases"&&x.IsUnique&&x.Columns.SequenceEqual(new[]{"PlayerProfileId","RequestId"}));
        operations.OfType<CreateIndexOperation>().Should().Contain(x=>x.Table=="ShopPurchases"&&x.IsUnique&&x.Columns.SequenceEqual(new[]{"PlayerProfileId","ItemId"}));
        operations.Should().NotContain(x=>x is UpdateDataOperation || x is DeleteDataOperation || x is InsertDataOperation);
    }
}
