using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflekt.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSprintToRetroSessionAddFokusLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_Cards_RetroSessions_ReviewedInSprintId",
                table: "Cards",
                column: "ReviewedInSprintId",
                principalTable: "RetroSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_RetroSessions_SprintId",
                table: "Cards",
                column: "SprintId",
                principalTable: "RetroSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_RetroSessions_ReviewedInSprintId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_RetroSessions_SprintId",
                table: "Cards");
        }
    }
}
