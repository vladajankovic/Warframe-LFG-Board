using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItemsStore.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResourceEntity",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceEntity", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceEntity_Name",
                table: "ResourceEntity",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceEntity");
        }
    }
}
