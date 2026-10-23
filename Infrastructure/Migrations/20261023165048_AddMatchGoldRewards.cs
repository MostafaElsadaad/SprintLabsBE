using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchGoldRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GoldGained",
                table: "MatchRewardResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NewGold",
                table: "MatchRewardResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OldGold",
                table: "MatchRewardResults",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoldGained",
                table: "MatchRewardResults");

            migrationBuilder.DropColumn(
                name: "NewGold",
                table: "MatchRewardResults");

            migrationBuilder.DropColumn(
                name: "OldGold",
                table: "MatchRewardResults");
        }
    }
}
