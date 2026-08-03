using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InviteOnlyTeacherAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordResetEmailSentAt",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherInvitations_CommunityUserId_AcceptedAt_RevokedAt_Expi~",
                table: "TeacherInvitations",
                columns: new[] { "CommunityUserId", "AcceptedAt", "RevokedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityUsers_UserId_Role_Status_CommunityId",
                table: "CommunityUsers",
                columns: new[] { "UserId", "Role", "Status", "CommunityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherInvitations_CommunityUserId_AcceptedAt_RevokedAt_Expi~",
                table: "TeacherInvitations");

            migrationBuilder.DropIndex(
                name: "IX_CommunityUsers_UserId_Role_Status_CommunityId",
                table: "CommunityUsers");

            migrationBuilder.DropColumn(
                name: "LastPasswordResetEmailSentAt",
                table: "Users");

        }
    }
}