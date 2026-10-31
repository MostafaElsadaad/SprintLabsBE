using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMagicalBoxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ShopProducts_DirectPrice",
                table: "ShopProducts");

            migrationBuilder.AddColumn<string>(
                name: "BoxKey",
                table: "ShopProducts",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CategoryOddsJson",
                table: "ShopProducts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DisplayLevel",
                table: "ShopProducts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconKey",
                table: "ShopProducts",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Items",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsBoxReward",
                table: "Items",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SellPrice",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PlayerBoxes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    ShopProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerBoxes", x => x.Id);
                    table.CheckConstraint("CK_PlayerBoxes_Quantity", "`Quantity` >= 0");
                    table.ForeignKey(
                        name: "FK_PlayerBoxes_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerBoxes_ShopProducts_ShopProductId",
                        column: x => x.ShopProductId,
                        principalTable: "ShopProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerCosmeticEquipment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerCosmeticEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerCosmeticEquipment_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerCosmeticEquipment_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopOperations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    RequestId = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetId = table.Column<long>(type: "bigint", nullable: false),
                    ResultJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopOperations_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ShopProducts_BoxKey",
                table: "ShopProducts",
                column: "BoxKey",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ShopProducts_DirectPrice",
                table: "ShopProducts",
                sql: "(`ItemId` IS NULL AND `CoinPrice` IS NULL AND `BoxKey` IS NULL) OR (`ItemId` IS NOT NULL AND `CoinPrice` IS NOT NULL AND `CoinPrice` > 0 AND `BoxKey` IS NULL) OR (`ItemId` IS NULL AND `CoinPrice` IS NOT NULL AND `CoinPrice` > 0 AND `BoxKey` IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Items_SellPrice",
                table: "Items",
                sql: "`SellPrice` >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBoxes_PlayerProfileId_ShopProductId",
                table: "PlayerBoxes",
                columns: new[] { "PlayerProfileId", "ShopProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBoxes_ShopProductId",
                table: "PlayerBoxes",
                column: "ShopProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCosmeticEquipment_ItemId",
                table: "PlayerCosmeticEquipment",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCosmeticEquipment_PlayerProfileId_Category",
                table: "PlayerCosmeticEquipment",
                columns: new[] { "PlayerProfileId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopOperations_PlayerProfileId_RequestId",
                table: "ShopOperations",
                columns: new[] { "PlayerProfileId", "RequestId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerBoxes");

            migrationBuilder.DropTable(
                name: "PlayerCosmeticEquipment");

            migrationBuilder.DropTable(
                name: "ShopOperations");

            migrationBuilder.DropIndex(
                name: "IX_ShopProducts_BoxKey",
                table: "ShopProducts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ShopProducts_DirectPrice",
                table: "ShopProducts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Items_SellPrice",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "BoxKey",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "CategoryOddsJson",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "DisplayLevel",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "IconKey",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IsBoxReward",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "SellPrice",
                table: "Items");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ShopProducts_DirectPrice",
                table: "ShopProducts",
                sql: "(`ItemId` IS NULL AND `CoinPrice` IS NULL) OR (`ItemId` IS NOT NULL AND `CoinPrice` IS NOT NULL AND `CoinPrice` > 0)");
        }
    }
}
