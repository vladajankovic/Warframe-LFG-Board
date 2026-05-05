using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItemsStore.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResourceName",
                table: "Resources",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ResourceName",
                table: "Resources",
                column: "ResourceName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resources_ResourceName",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ResourceName",
                table: "Resources");
        }
    }
}
