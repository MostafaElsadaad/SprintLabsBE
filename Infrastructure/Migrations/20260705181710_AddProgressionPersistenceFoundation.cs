using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressionPersistenceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HighestRankTier",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "RankTier",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Rp",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalMatches",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalWins",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MatchCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MirrorRoomId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CommunityId = table.Column<long>(type: "bigint", nullable: true),
                    MatchType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerXpLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    CommunityId = table.Column<long>(type: "bigint", nullable: true),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<long>(type: "bigint", nullable: true),
                    BaseXp = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    FinalXp = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerXpLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerXpLogs_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerXpLogs_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MatchPlayers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    CommunityId = table.Column<long>(type: "bigint", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    IsWinner = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "int", nullable: false),
                    WrongAnswers = table.Column<int>(type: "int", nullable: false),
                    MaxStreak = table.Column<int>(type: "int", nullable: false),
                    AnswerTimeTotalMs = table.Column<long>(type: "bigint", nullable: false),
                    QuestionTimeTotalMs = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchPlayers_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchPlayers_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchPlayers_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MatchQuestionResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<long>(type: "bigint", nullable: true),
                    QuestionType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AnswerTimeMs = table.Column<int>(type: "int", nullable: false),
                    QuestionTimeMs = table.Column<int>(type: "int", nullable: false),
                    StreakBeforeAnswer = table.Column<int>(type: "int", nullable: false),
                    StreakAfterAnswer = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchQuestionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchQuestionResults_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchQuestionResults_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MatchRewardResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    CommunityId = table.Column<long>(type: "bigint", nullable: true),
                    AnswerXp = table.Column<int>(type: "int", nullable: false),
                    MatchResultXp = table.Column<int>(type: "int", nullable: false),
                    MissionXp = table.Column<int>(type: "int", nullable: false),
                    TotalXp = table.Column<int>(type: "int", nullable: false),
                    OldLevel = table.Column<int>(type: "int", nullable: false),
                    NewLevel = table.Column<int>(type: "int", nullable: false),
                    OldTotalXp = table.Column<int>(type: "int", nullable: false),
                    NewTotalXp = table.Column<int>(type: "int", nullable: false),
                    RpChange = table.Column<int>(type: "int", nullable: false),
                    OldRp = table.Column<int>(type: "int", nullable: false),
                    NewRp = table.Column<int>(type: "int", nullable: false),
                    OldRankTier = table.Column<int>(type: "int", nullable: false),
                    NewRankTier = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchRewardResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchRewardResults_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchRewardResults_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchRewardResults_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerRankLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    CommunityId = table.Column<long>(type: "bigint", nullable: true),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    OldRp = table.Column<int>(type: "int", nullable: false),
                    RpChange = table.Column<int>(type: "int", nullable: false),
                    NewRp = table.Column<int>(type: "int", nullable: false),
                    OldRankTier = table.Column<int>(type: "int", nullable: false),
                    NewRankTier = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    TotalPlayers = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerRankLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerRankLogs_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerRankLogs_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerRankLogs_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Experience",
                table: "Players",
                column: "Experience");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Level",
                table: "Players",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Players_RankTier",
                table: "Players",
                column: "RankTier");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Rp",
                table: "Players",
                column: "Rp");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TotalWins",
                table: "Players",
                column: "TotalWins");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_CommunityId",
                table: "Matches",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_CompletedAt",
                table: "Matches",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MatchCode",
                table: "Matches",
                column: "MatchCode");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MirrorRoomId",
                table: "Matches",
                column: "MirrorRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Status",
                table: "Matches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayers_CommunityId",
                table: "MatchPlayers",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayers_MatchId",
                table: "MatchPlayers",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayers_MatchId_PlayerProfileId",
                table: "MatchPlayers",
                columns: new[] { "MatchId", "PlayerProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchPlayers_PlayerProfileId",
                table: "MatchPlayers",
                column: "PlayerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchQuestionResults_MatchId",
                table: "MatchQuestionResults",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchQuestionResults_PlayerProfileId",
                table: "MatchQuestionResults",
                column: "PlayerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchQuestionResults_QuestionId",
                table: "MatchQuestionResults",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRewardResults_CommunityId",
                table: "MatchRewardResults",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRewardResults_MatchId",
                table: "MatchRewardResults",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchRewardResults_MatchId_PlayerProfileId",
                table: "MatchRewardResults",
                columns: new[] { "MatchId", "PlayerProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchRewardResults_PlayerProfileId",
                table: "MatchRewardResults",
                column: "PlayerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRankLogs_CommunityId",
                table: "PlayerRankLogs",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRankLogs_CreatedAt",
                table: "PlayerRankLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRankLogs_MatchId",
                table: "PlayerRankLogs",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerRankLogs_PlayerProfileId",
                table: "PlayerRankLogs",
                column: "PlayerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerXpLogs_CommunityId",
                table: "PlayerXpLogs",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerXpLogs_CreatedAt",
                table: "PlayerXpLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerXpLogs_PlayerProfileId",
                table: "PlayerXpLogs",
                column: "PlayerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerXpLogs_SourceType",
                table: "PlayerXpLogs",
                column: "SourceType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchPlayers");

            migrationBuilder.DropTable(
                name: "MatchQuestionResults");

            migrationBuilder.DropTable(
                name: "MatchRewardResults");

            migrationBuilder.DropTable(
                name: "PlayerRankLogs");

            migrationBuilder.DropTable(
                name: "PlayerXpLogs");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Players_Experience",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_Level",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_RankTier",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_Rp",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_TotalWins",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "HighestRankTier",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "RankTier",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Rp",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TotalMatches",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "TotalWins",
                table: "Players");

        }
    }
}
