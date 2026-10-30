using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class SeedUnityCharacterCatalogue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddCharacter(migrationBuilder, "tech-smith", "Tech Smith");
            AddCharacter(migrationBuilder, "soul-mendrix", "Soul Mendrix");
        }

        private static void AddCharacter(MigrationBuilder migrationBuilder, string key, string name)
        {
            // Constants only. Preserve existing item metadata and administrator-edited listings.
            migrationBuilder.Sql($@"
INSERT INTO `Items` (`ItemKey`, `Name`, `IsCharacter`, `Rarity`, `IconKey`, `PrefabKey`)
SELECT '{key}', '{name}', 1, 'Common', 'shop/{key}', 'characters/{key}'
WHERE NOT EXISTS (SELECT 1 FROM `Items` WHERE `ItemKey` = '{key}');");
            migrationBuilder.Sql($@"
INSERT INTO `ShopProducts` (`Name`, `ItemId`, `CoinPrice`, `IsActive`)
SELECT '{name}', i.`Id`, 100, 1 FROM `Items` i
WHERE i.`ItemKey` = '{key}' AND i.`IsCharacter` = 1
AND NOT EXISTS (SELECT 1 FROM `ShopProducts` p WHERE p.`ItemId` = i.`Id`);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data is deliberately retained: purchases, inventory and equipped characters may
            // reference these IDs. Retire sales through the existing admin API if needed.
        }
    }
}
