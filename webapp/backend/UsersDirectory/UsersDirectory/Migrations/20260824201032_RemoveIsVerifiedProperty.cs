using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UsersDirectory.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsVerifiedProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                schema: "UsersDirectorySchema",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VerificationCode",
                schema: "UsersDirectorySchema",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                schema: "UsersDirectorySchema",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VerificationCode",
                schema: "UsersDirectorySchema",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }
    }
}
