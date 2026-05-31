using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItemsStore.Migrations
{
    /// <inheritdoc />
    public partial class AddSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ItemsStoreSchema");

            migrationBuilder.RenameTable(
                name: "Resources",
                newName: "Resources",
                newSchema: "ItemsStoreSchema");

            migrationBuilder.RenameTable(
                name: "Relics",
                newName: "Relics",
                newSchema: "ItemsStoreSchema");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Resources",
                schema: "ItemsStoreSchema",
                newName: "Resources");

            migrationBuilder.RenameTable(
                name: "Relics",
                schema: "ItemsStoreSchema",
                newName: "Relics");
        }
    }
}
