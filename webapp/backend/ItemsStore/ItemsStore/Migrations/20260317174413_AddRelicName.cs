using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItemsStore.Migrations
{
    /// <inheritdoc />
    public partial class AddRelicName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Relics_Name",
                table: "Relics");

            migrationBuilder.AddColumn<string>(
                name: "RelicName",
                table: "Relics",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Relics_RelicName",
                table: "Relics",
                column: "RelicName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Relics_RelicName",
                table: "Relics");

            migrationBuilder.DropColumn(
                name: "RelicName",
                table: "Relics");

            migrationBuilder.CreateIndex(
                name: "IX_Relics_Name",
                table: "Relics",
                column: "Name",
                unique: true);
        }
    }
}
