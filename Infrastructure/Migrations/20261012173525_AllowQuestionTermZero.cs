using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowQuestionTermZero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_Numbers",
                table: "Questions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_Numbers",
                table: "Questions",
                sql: "`Grade` > 0 AND `Term` >= 0 AND `Unit` > 0 AND `Lesson` > 0");
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
                sql: "`Grade` > 0 AND `Term` > 0 AND `Unit` > 0 AND `Lesson` > 0");
        }
    }
}
