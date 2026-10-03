using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MatchProgressionApiIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_MatchCode",
                table: "Matches");

            migrationBuilder.AddColumn<int>(
                name: "TotalPlayers",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MatchCode",
                table: "Matches",
                column: "MatchCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_MatchCode",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "TotalPlayers",
                table: "Matches");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MatchCode",
                table: "Matches",
                column: "MatchCode");
        }
    }
}
