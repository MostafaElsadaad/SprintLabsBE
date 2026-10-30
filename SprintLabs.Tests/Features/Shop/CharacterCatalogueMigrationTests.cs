using FluentAssertions;
using Infrastructure.DataAccess;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Compass.Tests.Features.Shop;

public class CharacterCatalogueMigrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Listings_are_repeat_safe_and_preserve_existing_admin_configuration(bool existing)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        Execute(connection, "CREATE TABLE Items (Id INTEGER PRIMARY KEY, ItemKey TEXT UNIQUE, Name TEXT, IsCharacter INTEGER, Rarity TEXT, IconKey TEXT, PrefabKey TEXT); CREATE TABLE ShopProducts (Id INTEGER PRIMARY KEY, Name TEXT, ItemId INTEGER UNIQUE, CoinPrice INTEGER, IsActive INTEGER);");
        if (existing)
            Execute(connection, "INSERT INTO Items VALUES (7, 'tech-smith', 'Custom Robot', 1, 'Rare', 'shop/robot', 'characters/robot'); INSERT INTO ShopProducts VALUES (8, 'Custom Robot', 7, 250, 0);");
        var type = typeof(ApplicationDbContext).Assembly.GetTypes().Single(x => x.Name == "SeedUnityCharacterCatalogue");
        var migration = (Migration)Activator.CreateInstance(type)!;
        foreach (var pass in new[] { 1, 2 })
            foreach (var operation in migration.UpOperations.Cast<SqlOperation>()) Execute(connection, operation.Sql);
        Scalar(connection, "SELECT COUNT(*) FROM Items").Should().Be(2);
        Scalar(connection, "SELECT COUNT(*) FROM ShopProducts").Should().Be(2);
        Scalar(connection, "SELECT CoinPrice FROM ShopProducts WHERE ItemId = (SELECT Id FROM Items WHERE ItemKey='tech-smith')").Should().Be(existing ? 250 : 100);
        Scalar(connection, "SELECT IsActive FROM ShopProducts WHERE ItemId = (SELECT Id FROM Items WHERE ItemKey='tech-smith')").Should().Be(existing ? 0 : 1);
        Scalar(connection, "SELECT CoinPrice FROM ShopProducts WHERE ItemId = (SELECT Id FROM Items WHERE ItemKey='soul-mendrix')").Should().Be(100);
        Scalar(connection, "SELECT COUNT(*) FROM Items WHERE ItemKey='soul-mendrix' AND IsCharacter=1 AND IconKey='shop/soul-mendrix' AND PrefabKey='characters/soul-mendrix'").Should().Be(1);
        // Rollback must retain products referenced by purchases, ownership or equipped selections.
        migration.DownOperations.Should().BeEmpty();
    }

    [Fact]
    public void Existing_non_character_item_is_not_repurposed_or_offered_for_sale()
    {
        using var connection = new SqliteConnection("Data Source=:memory:"); connection.Open();
        Execute(connection, "CREATE TABLE Items (Id INTEGER PRIMARY KEY, ItemKey TEXT UNIQUE, Name TEXT, IsCharacter INTEGER, Rarity TEXT, IconKey TEXT, PrefabKey TEXT); CREATE TABLE ShopProducts (Id INTEGER PRIMARY KEY, Name TEXT, ItemId INTEGER UNIQUE, CoinPrice INTEGER, IsActive INTEGER); INSERT INTO Items VALUES (3, 'tech-smith', 'Mission item', 0, 'Common', '', '');");
        var type = typeof(ApplicationDbContext).Assembly.GetTypes().Single(x => x.Name == "SeedUnityCharacterCatalogue");
        foreach (var operation in ((Migration)Activator.CreateInstance(type)!).UpOperations.Cast<SqlOperation>()) Execute(connection, operation.Sql);
        Scalar(connection, "SELECT IsCharacter FROM Items WHERE Id=3").Should().Be(0);
        Scalar(connection, "SELECT COUNT(*) FROM ShopProducts WHERE ItemId=3").Should().Be(0);
    }

    private static void Execute(SqliteConnection connection, string sql)
    { using var command = connection.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery(); }
    private static long Scalar(SqliteConnection connection, string sql)
    { using var command = connection.CreateCommand(); command.CommandText = sql; return Convert.ToInt64(command.ExecuteScalar()); }
}
