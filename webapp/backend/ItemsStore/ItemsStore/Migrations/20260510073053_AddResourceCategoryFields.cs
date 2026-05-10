using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItemsStore.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceCategoryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Resources",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "Resources",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WikiCategory",
                table: "Resources",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "WikiCategory",
                table: "Resources");
        }
    }
}
