using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflekt.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReflektUserUnifyIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Participants_Users_UserId",
                table: "Participants");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Users_OwnerId",
                table: "Teams");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Participants_SprintId_UserId",
                table: "Participants");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Participants",
                newName: "PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_Participants_UserId",
                table: "Participants",
                newName: "IX_Participants_PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_SprintId_PersonId",
                table: "Participants",
                columns: new[] { "SprintId", "PersonId" },
                unique: true,
                filter: "\"PersonId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Participants_SprintId_PersonId",
                table: "Participants");

            migrationBuilder.RenameColumn(
                name: "PersonId",
                table: "Participants",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Participants_PersonId",
                table: "Participants",
                newName: "IX_Participants_UserId");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedById = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false, collation: "NOCASE"),
                    GivenName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastModifiedById = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Participants_SprintId_UserId",
                table: "Participants",
                columns: new[] { "SprintId", "UserId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Participants_Users_UserId",
                table: "Participants",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Users_OwnerId",
                table: "Teams",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
