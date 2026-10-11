using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuestionTimingSeconds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_QuestionHistory_Time",
                table: "QuestionHistory");

            migrationBuilder.RenameColumn(
                name: "TimeTakenMs",
                table: "QuestionHistory",
                newName: "TimeTakenSeconds");

            migrationBuilder.AddColumn<decimal>(
                name: "TimerSeconds",
                table: "Questions",
                type: "decimal(10,3)",
                precision: 10,
                scale: 3,
                nullable: false,
                defaultValue: 10m);

            // Widen first so every existing Int32 millisecond value fits before conversion.
            migrationBuilder.AlterColumn<decimal>(
                name: "TimeTakenSeconds",
                table: "QuestionHistory",
                type: "decimal(13,3)",
                precision: 13,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql("UPDATE `QuestionHistory` SET `TimeTakenSeconds` = `TimeTakenSeconds` / 1000;");

            migrationBuilder.AlterColumn<decimal>(
                name: "TimeTakenSeconds", table: "QuestionHistory", type: "decimal(10,3)",
                precision: 10, scale: 3, nullable: false,
                oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Questions_TimerSeconds",
                table: "Questions",
                sql: "`TimerSeconds` > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuestionHistory_Time",
                table: "QuestionHistory",
                sql: "`TimeTakenSeconds` >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reject values the older Int32-millisecond schema cannot represent before changing data.
            migrationBuilder.AddCheckConstraint(
                name: "CK_QuestionHistory_RollbackRange", table: "QuestionHistory",
                sql: "`TimeTakenSeconds` <= 2147483.647");
            migrationBuilder.DropCheckConstraint(name: "CK_QuestionHistory_RollbackRange", table: "QuestionHistory");
            migrationBuilder.DropCheckConstraint(
                name: "CK_Questions_TimerSeconds",
                table: "Questions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_QuestionHistory_Time",
                table: "QuestionHistory");

            migrationBuilder.DropColumn(
                name: "TimerSeconds",
                table: "Questions");

            migrationBuilder.AlterColumn<decimal>(
                name: "TimeTakenSeconds", table: "QuestionHistory", type: "decimal(13,3)",
                precision: 13, scale: 3, nullable: false,
                oldClrType: typeof(decimal), oldType: "decimal(10,3)", oldPrecision: 10, oldScale: 3);
            migrationBuilder.Sql("UPDATE `QuestionHistory` SET `TimeTakenSeconds` = `TimeTakenSeconds` * 1000;");
            migrationBuilder.AlterColumn<int>(name: "TimeTakenSeconds", table: "QuestionHistory", type: "int", nullable: false,
                oldClrType: typeof(decimal), oldType: "decimal(13,3)", oldPrecision: 13, oldScale: 3);
            migrationBuilder.RenameColumn(name: "TimeTakenSeconds", table: "QuestionHistory", newName: "TimeTakenMs");

            migrationBuilder.AddCheckConstraint(
                name: "CK_QuestionHistory_Time",
                table: "QuestionHistory",
                sql: "`TimeTakenMs` >= 0");
        }
    }
}
