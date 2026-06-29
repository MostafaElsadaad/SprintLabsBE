using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OwnerStudentLicenseManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentLicenses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CommunityId = table.Column<long>(type: "bigint", nullable: false),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: true),
                    GradeId = table.Column<long>(type: "bigint", nullable: false),
                    ClassId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    EmailChangeCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AssignedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentLicenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentLicenses_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentLicenses_Communities_CommunityId",
                        column: x => x.CommunityId,
                        principalTable: "Communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentLicenses_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentLicenses_Players_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentLicenses_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentLicenses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLicenses_AssignedByUserId",
                table: "StudentLicenses",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLicenses_ClassId",
                table: "StudentLicenses",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLicenses_CommunityId",
                table: "StudentLicenses",
                column: "CommunityId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLicenses_CommunityId_ClassId",
                table: "StudentLicenses",
                columns: new[] { "CommunityId", "ClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentLicenses_CommunityId_Email_Status",
                table: "StudentLicenses",
                columns: new[] { "CommunityId", "Email", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentLicenses_CommunityId_GradeId",
                table: "StudentLicenses",
                columns: new[] { "CommunityId", "GradeId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentLicenses_CommunityId_Status",
                table: "StudentLicenses",
                columns: new[] { "CommunityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentLicenses_GradeId",
                table: "StudentLicenses",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLicenses_PlayerProfileId",
                table: "StudentLicenses",
                column: "PlayerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLicenses_UserId",
                table: "StudentLicenses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentLicenses");

        }
    }
}
