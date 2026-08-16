using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItemsStore.Migrations
{
    /// <inheritdoc />
    public partial class AddRelicDrop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RelicDrops",
                schema: "ItemsStoreSchema",
                columns: table => new
                {
                    RelicId = table.Column<int>(type: "integer", nullable: false),
                    ItemName = table.Column<string>(type: "text", nullable: false),
                    Rarity = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelicDrops", x => new { x.RelicId, x.ItemName });
                    table.ForeignKey(
                        name: "FK_RelicDrops_Relics_RelicId",
                        column: x => x.RelicId,
                        principalSchema: "ItemsStoreSchema",
                        principalTable: "Relics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelicDrops",
                schema: "ItemsStoreSchema");
        }
    }
}
