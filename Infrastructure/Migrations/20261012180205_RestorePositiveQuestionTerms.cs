using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestorePositiveQuestionTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Validate existing rows and prevent new Term 0 rows before replacing the old constraint.
            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_PositiveTermUpgrade",
                table: "Questions",
                sql: "`Term` > 0");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_Numbers",
                table: "Questions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_Numbers",
                table: "Questions",
                sql: "`Grade` > 0 AND `Term` > 0 AND `Unit` > 0 AND `Lesson` > 0");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_PositiveTermUpgrade",
                table: "Questions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_Numbers",
                table: "Questions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_Numbers",
                table: "Questions",
                sql: "`Grade` > 0 AND `Term` >= 0 AND `Unit` > 0 AND `Lesson` > 0");
        }
    }
}
