using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MatchAnswerOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                table: "MatchQuestionResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MatchQuestionResults_MatchId_PlayerProfileId_Sequence",
                table: "MatchQuestionResults",
                columns: new[] { "MatchId", "PlayerProfileId", "Sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchQuestionResults_MatchId_PlayerProfileId_Sequence",
                table: "MatchQuestionResults");

            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "MatchQuestionResults");
        }
    }
}
