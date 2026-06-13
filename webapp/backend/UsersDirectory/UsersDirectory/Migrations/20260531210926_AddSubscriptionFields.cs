using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UsersDirectory.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionExpiryDate",
                schema: "UsersDirectorySchema",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                schema: "UsersDirectorySchema",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "Inactive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionExpiryDate",
                schema: "UsersDirectorySchema",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                schema: "UsersDirectorySchema",
                table: "Users");
        }
    }
}
