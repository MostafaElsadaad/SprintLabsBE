using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommunityTermReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "TermEndDate",
                table: "Communities",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TermName",
                table: "Communities",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateOnly>(
                name: "TermStartDate",
                table: "Communities",
                type: "date",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Communities_TermDates",
                table: "Communities",
                sql: "(`TermName` IS NULL AND `TermStartDate` IS NULL AND `TermEndDate` IS NULL) OR (`TermName` IS NOT NULL AND LENGTH(TRIM(`TermName`)) > 0 AND `TermStartDate` IS NOT NULL AND `TermEndDate` IS NOT NULL AND `TermStartDate` >= '1000-01-01' AND `TermEndDate` >= `TermStartDate` AND `TermEndDate` < '9999-12-31')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Communities_TermDates",
                table: "Communities");

            migrationBuilder.DropColumn(
                name: "TermEndDate",
                table: "Communities");

            migrationBuilder.DropColumn(
                name: "TermName",
                table: "Communities");

            migrationBuilder.DropColumn(
                name: "TermStartDate",
                table: "Communities");
        }
    }
}
