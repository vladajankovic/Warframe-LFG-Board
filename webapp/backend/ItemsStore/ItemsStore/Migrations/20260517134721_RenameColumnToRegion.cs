using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItemsStore.Migrations
{
    /// <inheritdoc />
    public partial class RenameColumnToRegion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WikiCategory",
                table: "Resources");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Resources",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Resources",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Region",
                table: "Resources");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Resources",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WikiCategory",
                table: "Resources",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }
    }
}
