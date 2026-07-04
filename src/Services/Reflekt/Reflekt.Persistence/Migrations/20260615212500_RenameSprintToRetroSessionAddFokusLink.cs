using System;
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
            migrationBuilder.DropForeignKey(
                name: "FK_CardGroups_Sprints_SprintId",
                table: "CardGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Sprints_ReviewedInSprintId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Sprints_SprintId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Participants_Sprints_SprintId",
                table: "Participants");

            migrationBuilder.DropForeignKey(
                name: "FK_Sprints_Teams_TeamId",
                table: "Sprints");

            migrationBuilder.RenameTable(
                name: "Sprints",
                newName: "RetroSessions");

            migrationBuilder.RenameIndex(
                name: "IX_Sprints_TeamId",
                table: "RetroSessions",
                newName: "IX_RetroSessions_TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_Sprints_ClosedAt",
                table: "RetroSessions",
                newName: "IX_RetroSessions_ClosedAt");

            migrationBuilder.CreateTable(
                name: "FokusSprintRefs",
                columns: table => new
                {
                    RetroSessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    FokusSprintId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FokusSprintRefs", x => new { x.RetroSessionId, x.FokusSprintId });
                    table.ForeignKey(
                        name: "FK_FokusSprintRefs_RetroSessions_RetroSessionId",
                        column: x => x.RetroSessionId,
                        principalTable: "RetroSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FokusSprintSnapshots",
                columns: table => new
                {
                    SprintId = table.Column<int>(type: "INTEGER", nullable: false),
                    BoardId = table.Column<int>(type: "INTEGER", nullable: false),
                    SprintName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FokusSprintSnapshots", x => x.SprintId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FokusSprintRefs_FokusSprintId",
                table: "FokusSprintRefs",
                column: "FokusSprintId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_RetroSessions_Teams_TeamId",
                table: "RetroSessions",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropForeignKey(
                name: "FK_RetroSessions_Teams_TeamId",
                table: "RetroSessions");

            migrationBuilder.DropTable(
                name: "FokusSprintRefs");

            migrationBuilder.DropTable(
                name: "FokusSprintSnapshots");

            migrationBuilder.RenameTable(
                name: "RetroSessions",
                newName: "Sprints");

            migrationBuilder.RenameIndex(
                name: "IX_RetroSessions_TeamId",
                table: "Sprints",
                newName: "IX_Sprints_TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_RetroSessions_ClosedAt",
                table: "Sprints",
                newName: "IX_Sprints_ClosedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_CardGroups_Sprints_SprintId",
                table: "CardGroups",
                column: "SprintId",
                principalTable: "Sprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Sprints_ReviewedInSprintId",
                table: "Cards",
                column: "ReviewedInSprintId",
                principalTable: "Sprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Sprints_SprintId",
                table: "Cards",
                column: "SprintId",
                principalTable: "Sprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_Sprints_SprintId",
                table: "Participants",
                column: "SprintId",
                principalTable: "Sprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sprints_Teams_TeamId",
                table: "Sprints",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
