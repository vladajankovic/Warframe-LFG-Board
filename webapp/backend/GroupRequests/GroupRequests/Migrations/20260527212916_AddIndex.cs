using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupRequests.Migrations
{
    /// <inheritdoc />
    public partial class AddIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GroupRequests_UserId_ItemName",
                table: "GroupRequests",
                columns: new[] { "UserId", "ItemName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GroupRequests_UserId_ItemName",
                table: "GroupRequests");
        }
    }
}
