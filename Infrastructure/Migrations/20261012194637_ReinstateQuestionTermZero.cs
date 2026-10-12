using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReinstateQuestionTermZero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_NonnegativeTermUpgrade",
                table: "Questions",
                sql: "`Grade` > 0 AND `Term` >= 0 AND `Unit` > 0 AND `Lesson` > 0");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_Numbers",
                table: "Questions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_Numbers",
                table: "Questions",
                sql: "`Grade` > 0 AND `Term` >= 0 AND `Unit` > 0 AND `Lesson` > 0");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_NonnegativeTermUpgrade",
                table: "Questions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Stop rollback before replacing the current constraint if Term 0 rows exist.
            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_PositiveTermRollback",
                table: "Questions",
                sql: "`Grade` > 0 AND `Term` > 0 AND `Unit` > 0 AND `Lesson` > 0");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_Numbers",
                table: "Questions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_Numbers",
                table: "Questions",
                sql: "`Grade` > 0 AND `Term` > 0 AND `Unit` > 0 AND `Lesson` > 0");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_PositiveTermRollback",
                table: "Questions");
        }
    }
}
