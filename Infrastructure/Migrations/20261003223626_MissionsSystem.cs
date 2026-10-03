using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MissionsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ItemKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rarity = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IconKey = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrefabKey = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MissionActivations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PeriodType = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RandomMissionCount = table.Column<int>(type: "int", nullable: false),
                    AutoClaimCompletedOnReset = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionActivations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MissionEventLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EventId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    CommunityId = table.Column<long>(type: "bigint", nullable: true),
                    MatchId = table.Column<long>(type: "bigint", nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    EventDataJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OccurredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResultJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionEventLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionEventLogs_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionEventLogs_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionEventLogs_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MissionTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MissionKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<int>(type: "int", nullable: false),
                    PeriodType = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    ProgressType = table.Column<int>(type: "int", nullable: false),
                    TargetValue = table.Column<int>(type: "int", nullable: false),
                    ConditionsJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProgressField = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResetEventType = table.Column<int>(type: "int", nullable: true),
                    MatchScoped = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RepeatValue = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionTemplates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShopProducts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopProducts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerInventoryItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerInventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerInventoryItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerInventoryItems_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerMissionAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    MissionActivationId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMissionAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerMissionAssignments_MissionActivations_MissionActivatio~",
                        column: x => x.MissionActivationId,
                        principalTable: "MissionActivations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerMissionAssignments_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MissionActivationItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MissionActivationId = table.Column<long>(type: "bigint", nullable: false),
                    MissionTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    AssignmentMode = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionActivationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionActivationItems_MissionActivations_MissionActivationId",
                        column: x => x.MissionActivationId,
                        principalTable: "MissionActivations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionActivationItems_MissionTemplates_MissionTemplateId",
                        column: x => x.MissionTemplateId,
                        principalTable: "MissionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerMissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    MissionActivationId = table.Column<long>(type: "bigint", nullable: false),
                    MissionTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentProgress = table.Column<int>(type: "int", nullable: false),
                    TargetProgress = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProgressStateJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefinitionSnapshotJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaimSnapshotJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastEventAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerMissions_MissionActivations_MissionActivationId",
                        column: x => x.MissionActivationId,
                        principalTable: "MissionActivations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerMissions_MissionTemplates_MissionTemplateId",
                        column: x => x.MissionTemplateId,
                        principalTable: "MissionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerMissions_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BoxRewardEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ShopProductId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoxRewardEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoxRewardEntries_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoxRewardEntries_ShopProducts_ShopProductId",
                        column: x => x.ShopProductId,
                        principalTable: "ShopProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MissionRewards",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MissionTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    RewardType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: true),
                    ShopProductId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionRewards_MissionTemplates_MissionTemplateId",
                        column: x => x.MissionTemplateId,
                        principalTable: "MissionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionRewards_ShopProducts_ShopProductId",
                        column: x => x.ShopProductId,
                        principalTable: "ShopProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MissionClaimLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerMissionId = table.Column<long>(type: "bigint", nullable: false),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    MissionTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    RewardType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    ShopProductId = table.Column<long>(type: "bigint", nullable: true),
                    RewardItemId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionClaimLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionClaimLogs_Items_RewardItemId",
                        column: x => x.RewardItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionClaimLogs_MissionTemplates_MissionTemplateId",
                        column: x => x.MissionTemplateId,
                        principalTable: "MissionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionClaimLogs_PlayerMissions_PlayerMissionId",
                        column: x => x.PlayerMissionId,
                        principalTable: "PlayerMissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionClaimLogs_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionClaimLogs_ShopProducts_ShopProductId",
                        column: x => x.ShopProductId,
                        principalTable: "ShopProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BoxRewardEntries_ItemId",
                table: "BoxRewardEntries",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BoxRewardEntries_ShopProductId",
                table: "BoxRewardEntries",
                column: "ShopProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemKey",
                table: "Items",
                column: "ItemKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionActivationItems_MissionActivationId_MissionTemplateId",
                table: "MissionActivationItems",
                columns: new[] { "MissionActivationId", "MissionTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionActivationItems_MissionTemplateId",
                table: "MissionActivationItems",
                column: "MissionTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionActivations_Name",
                table: "MissionActivations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionActivations_Status_EndsAt",
                table: "MissionActivations",
                columns: new[] { "Status", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MissionClaimLogs_MissionTemplateId",
                table: "MissionClaimLogs",
                column: "MissionTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionClaimLogs_PlayerMissionId",
                table: "MissionClaimLogs",
                column: "PlayerMissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionClaimLogs_PlayerProfileId",
                table: "MissionClaimLogs",
                column: "PlayerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionClaimLogs_RewardItemId",
                table: "MissionClaimLogs",
                column: "RewardItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionClaimLogs_ShopProductId",
                table: "MissionClaimLogs",
                column: "ShopProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionEventLogs_CommunityId",
                table: "MissionEventLogs",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionEventLogs_MatchId",
                table: "MissionEventLogs",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionEventLogs_PlayerProfileId_EventId",
                table: "MissionEventLogs",
                columns: new[] { "PlayerProfileId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissionRewards_MissionTemplateId",
                table: "MissionRewards",
                column: "MissionTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionRewards_ShopProductId",
                table: "MissionRewards",
                column: "ShopProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionTemplates_MissionKey",
                table: "MissionTemplates",
                column: "MissionKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInventoryItems_ItemId",
                table: "PlayerInventoryItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInventoryItems_PlayerProfileId_ItemId",
                table: "PlayerInventoryItems",
                columns: new[] { "PlayerProfileId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMissionAssignments_MissionActivationId",
                table: "PlayerMissionAssignments",
                column: "MissionActivationId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMissionAssignments_PlayerProfileId_MissionActivationId",
                table: "PlayerMissionAssignments",
                columns: new[] { "PlayerProfileId", "MissionActivationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMissions_MissionActivationId_Status",
                table: "PlayerMissions",
                columns: new[] { "MissionActivationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMissions_MissionTemplateId",
                table: "PlayerMissions",
                column: "MissionTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMissions_PlayerProfileId_MissionActivationId_MissionTe~",
                table: "PlayerMissions",
                columns: new[] { "PlayerProfileId", "MissionActivationId", "MissionTemplateId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoxRewardEntries");

            migrationBuilder.DropTable(
                name: "MissionActivationItems");

            migrationBuilder.DropTable(
                name: "MissionClaimLogs");

            migrationBuilder.DropTable(
                name: "MissionEventLogs");

            migrationBuilder.DropTable(
                name: "MissionRewards");

            migrationBuilder.DropTable(
                name: "PlayerInventoryItems");

            migrationBuilder.DropTable(
                name: "PlayerMissionAssignments");

            migrationBuilder.DropTable(
                name: "PlayerMissions");

            migrationBuilder.DropTable(
                name: "ShopProducts");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "MissionActivations");

            migrationBuilder.DropTable(
                name: "MissionTemplates");
        }
    }
}
