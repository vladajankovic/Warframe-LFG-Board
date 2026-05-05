using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItemsStore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ResourceEntity",
                table: "ResourceEntity");

            migrationBuilder.RenameTable(
                name: "ResourceEntity",
                newName: "Resources");

            migrationBuilder.RenameIndex(
                name: "IX_ResourceEntity_Name",
                table: "Resources",
                newName: "IX_Resources_Name");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Resources",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Resources",
                table: "Resources",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Resources",
                table: "Resources");

            migrationBuilder.RenameTable(
                name: "Resources",
                newName: "ResourceEntity");

            migrationBuilder.RenameIndex(
                name: "IX_Resources_Name",
                table: "ResourceEntity",
                newName: "IX_ResourceEntity_Name");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ResourceEntity",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ResourceEntity",
                table: "ResourceEntity",
                column: "Id");
        }
    }
}
