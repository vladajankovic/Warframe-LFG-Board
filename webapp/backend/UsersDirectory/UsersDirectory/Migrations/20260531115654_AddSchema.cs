using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UsersDirectory.Migrations
{
    /// <inheritdoc />
    public partial class AddSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "UsersDirectorySchema");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "Users",
                newSchema: "UsersDirectorySchema");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Users",
                schema: "UsersDirectorySchema",
                newName: "Users");
        }
    }
}
