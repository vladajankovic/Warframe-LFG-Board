using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UsersDirectory.Migrations
{
    /// <inheritdoc />
    public partial class AddInGameName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InGameName",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InGameName",
                table: "Users");
        }
    }
}
