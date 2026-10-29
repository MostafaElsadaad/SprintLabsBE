using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoinShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoinPrice",
                table: "ShopProducts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ItemId",
                table: "ShopProducts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EquippedCharacterItemId",
                table: "Players",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCharacter",
                table: "Items",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ShopPurchases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    ShopProductId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    RequestId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CoinsSpent = table.Column<int>(type: "int", nullable: false),
                    CoinsAfter = table.Column<int>(type: "int", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopPurchases", x => x.Id);
                    table.CheckConstraint("CK_ShopPurchases_Coins", "`CoinsSpent` > 0 AND `CoinsAfter` >= 0");
                    table.ForeignKey(
                        name: "FK_ShopPurchases_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopPurchases_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShopPurchases_ShopProducts_ShopProductId",
                        column: x => x.ShopProductId,
                        principalTable: "ShopProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ShopProducts_ItemId",
                table: "ShopProducts",
                column: "ItemId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ShopProducts_DirectPrice",
                table: "ShopProducts",
                sql: "(`ItemId` IS NULL AND `CoinPrice` IS NULL) OR (`ItemId` IS NOT NULL AND `CoinPrice` IS NOT NULL AND `CoinPrice` > 0)");

            migrationBuilder.CreateIndex(
                name: "IX_Players_EquippedCharacterItemId",
                table: "Players",
                column: "EquippedCharacterItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchases_ItemId",
                table: "ShopPurchases",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchases_PlayerProfileId_Id",
                table: "ShopPurchases",
                columns: new[] { "PlayerProfileId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchases_PlayerProfileId_ItemId",
                table: "ShopPurchases",
                columns: new[] { "PlayerProfileId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchases_PlayerProfileId_RequestId",
                table: "ShopPurchases",
                columns: new[] { "PlayerProfileId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopPurchases_ShopProductId",
                table: "ShopPurchases",
                column: "ShopProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Items_EquippedCharacterItemId",
                table: "Players",
                column: "EquippedCharacterItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ShopProducts_Items_ItemId",
                table: "ShopProducts",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Items_EquippedCharacterItemId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_ShopProducts_Items_ItemId",
                table: "ShopProducts");

            migrationBuilder.DropTable(
                name: "ShopPurchases");

            migrationBuilder.DropIndex(
                name: "IX_ShopProducts_ItemId",
                table: "ShopProducts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ShopProducts_DirectPrice",
                table: "ShopProducts");

            migrationBuilder.DropIndex(
                name: "IX_Players_EquippedCharacterItemId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CoinPrice",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "EquippedCharacterItemId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "IsCharacter",
                table: "Items");
        }
    }
}
