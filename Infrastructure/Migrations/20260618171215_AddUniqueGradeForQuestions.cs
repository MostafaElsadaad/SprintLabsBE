using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueGradeForQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_Grade_Assignment_Version",
                table: "Questions");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 18, 17, 12, 12, 407, DateTimeKind.Utc).AddTicks(6299), new DateTime(2026, 6, 18, 17, 12, 12, 407, DateTimeKind.Utc).AddTicks(6299) });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Grade",
                table: "Questions",
                column: "Grade",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_Grade",
                table: "Questions");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 4, 30, 10, 54, 38, 593, DateTimeKind.Utc).AddTicks(399), new DateTime(2026, 4, 30, 10, 54, 38, 593, DateTimeKind.Utc).AddTicks(399) });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Grade_Assignment_Version",
                table: "Questions",
                columns: new[] { "Grade", "Assignment", "Version" },
                unique: true);
        }
    }
}
