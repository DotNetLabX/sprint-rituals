using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflekt.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameBoardFkSprintIdToRetroSessionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardGroups_RetroSessions_SprintId",
                table: "CardGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_RetroSessions_ReviewedInSprintId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_RetroSessions_SprintId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Participants_RetroSessions_SprintId",
                table: "Participants");

            migrationBuilder.RenameColumn(
                name: "SprintId",
                table: "Participants",
                newName: "RetroSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Participants_SprintId_PersonId",
                table: "Participants",
                newName: "IX_Participants_RetroSessionId_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_Participants_SprintId",
                table: "Participants",
                newName: "IX_Participants_RetroSessionId");

            migrationBuilder.RenameColumn(
                name: "SprintId",
                table: "Cards",
                newName: "RetroSessionId");

            migrationBuilder.RenameColumn(
                name: "ReviewedInSprintId",
                table: "Cards",
                newName: "ReviewedInRetroSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Cards_SprintId",
                table: "Cards",
                newName: "IX_Cards_RetroSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_Cards_ReviewedInSprintId",
                table: "Cards",
                newName: "IX_Cards_ReviewedInRetroSessionId");

            migrationBuilder.RenameColumn(
                name: "SprintId",
                table: "CardGroups",
                newName: "RetroSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_CardGroups_SprintId",
                table: "CardGroups",
                newName: "IX_CardGroups_RetroSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardGroups_RetroSessions_RetroSessionId",
                table: "CardGroups",
                column: "RetroSessionId",
                principalTable: "RetroSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_RetroSessions_RetroSessionId",
                table: "Cards",
                column: "RetroSessionId",
                principalTable: "RetroSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_RetroSessions_ReviewedInRetroSessionId",
                table: "Cards",
                column: "ReviewedInRetroSessionId",
                principalTable: "RetroSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_RetroSessions_RetroSessionId",
                table: "Participants",
                column: "RetroSessionId",
                principalTable: "RetroSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardGroups_RetroSessions_RetroSessionId",
                table: "CardGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_RetroSessions_RetroSessionId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_RetroSessions_ReviewedInRetroSessionId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Participants_RetroSessions_RetroSessionId",
                table: "Participants");

            migrationBuilder.RenameColumn(
                name: "RetroSessionId",
                table: "Participants",
                newName: "SprintId");

            migrationBuilder.RenameIndex(
                name: "IX_Participants_RetroSessionId_PersonId",
                table: "Participants",
                newName: "IX_Participants_SprintId_PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_Participants_RetroSessionId",
                table: "Participants",
                newName: "IX_Participants_SprintId");

            migrationBuilder.RenameColumn(
                name: "ReviewedInRetroSessionId",
                table: "Cards",
                newName: "ReviewedInSprintId");

            migrationBuilder.RenameColumn(
                name: "RetroSessionId",
                table: "Cards",
                newName: "SprintId");

            migrationBuilder.RenameIndex(
                name: "IX_Cards_ReviewedInRetroSessionId",
                table: "Cards",
                newName: "IX_Cards_ReviewedInSprintId");

            migrationBuilder.RenameIndex(
                name: "IX_Cards_RetroSessionId",
                table: "Cards",
                newName: "IX_Cards_SprintId");

            migrationBuilder.RenameColumn(
                name: "RetroSessionId",
                table: "CardGroups",
                newName: "SprintId");

            migrationBuilder.RenameIndex(
                name: "IX_CardGroups_RetroSessionId",
                table: "CardGroups",
                newName: "IX_CardGroups_SprintId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardGroups_RetroSessions_SprintId",
                table: "CardGroups",
                column: "SprintId",
                principalTable: "RetroSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_RetroSessions_SprintId",
                table: "Participants",
                column: "SprintId",
                principalTable: "RetroSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
