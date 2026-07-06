using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserNotification.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDismissals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupRequestUserId",
                schema: "UserNotificationsSchema",
                table: "Notifications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "NotificationDismissals",
                schema: "UserNotificationsSchema",
                columns: table => new
                {
                    NotificationId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDismissals", x => new { x.NotificationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_NotificationDismissals_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalSchema: "UserNotificationsSchema",
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ItemName_CreatedAt",
                schema: "UserNotificationsSchema",
                table: "Notifications",
                columns: new[] { "ItemName", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDismissals_UserId",
                schema: "UserNotificationsSchema",
                table: "NotificationDismissals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDismissals",
                schema: "UserNotificationsSchema");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ItemName_CreatedAt",
                schema: "UserNotificationsSchema",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "GroupRequestUserId",
                schema: "UserNotificationsSchema",
                table: "Notifications");
        }
    }
}
