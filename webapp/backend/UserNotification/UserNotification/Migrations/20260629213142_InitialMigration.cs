using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserNotification.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "UserNotificationsSchema");

            migrationBuilder.CreateTable(
                name: "Favorites",
                schema: "UserNotificationsSchema",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "UserNotificationsSchema",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    InGameName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_ItemName",
                schema: "UserNotificationsSchema",
                table: "Favorites",
                columns: new[] { "UserId", "ItemName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Favorites",
                schema: "UserNotificationsSchema");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "UserNotificationsSchema");
        }
    }
}
