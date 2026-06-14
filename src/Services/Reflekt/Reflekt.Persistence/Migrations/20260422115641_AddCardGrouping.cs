using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Reflekt.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCardGrouping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Votes_Cards_CardId",
                table: "Votes");

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 6, 1 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 7, 1 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 3, 2 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 4, 2 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 5, 2 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 8, 2 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 9, 2 });

            migrationBuilder.RenameColumn(
                name: "CardId",
                table: "Votes",
                newName: "GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_Votes_CardId",
                table: "Votes",
                newName: "IX_Votes_GroupId");

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "Cards",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CardGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SprintId = table.Column<int>(type: "INTEGER", nullable: false),
                    ColumnType = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardGroups_Sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "Sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "PhaseActions",
                columns: new[] { "Action", "Phase" },
                values: new object[,]
                {
                    { 11, 1 },
                    { 12, 1 },
                    { 13, 1 },
                    { 6, 2 },
                    { 7, 2 },
                    { 3, 3 },
                    { 4, 3 },
                    { 5, 3 },
                    { 8, 3 },
                    { 9, 3 },
                    { 10, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_GroupId",
                table: "Cards",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CardGroups_SprintId",
                table: "CardGroups",
                column: "SprintId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_CardGroups_GroupId",
                table: "Cards",
                column: "GroupId",
                principalTable: "CardGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_CardGroups_GroupId",
                table: "Votes",
                column: "GroupId",
                principalTable: "CardGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_CardGroups_GroupId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_CardGroups_GroupId",
                table: "Votes");

            migrationBuilder.DropTable(
                name: "CardGroups");

            migrationBuilder.DropIndex(
                name: "IX_Cards_GroupId",
                table: "Cards");

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 11, 1 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 12, 1 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 13, 1 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 6, 2 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 7, 2 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 3, 3 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 4, 3 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 5, 3 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 8, 3 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 9, 3 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 10, 3 });

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Cards");

            migrationBuilder.RenameColumn(
                name: "GroupId",
                table: "Votes",
                newName: "CardId");

            migrationBuilder.RenameIndex(
                name: "IX_Votes_GroupId",
                table: "Votes",
                newName: "IX_Votes_CardId");

            migrationBuilder.InsertData(
                table: "PhaseActions",
                columns: new[] { "Action", "Phase" },
                values: new object[,]
                {
                    { 6, 1 },
                    { 7, 1 },
                    { 3, 2 },
                    { 4, 2 },
                    { 5, 2 },
                    { 8, 2 },
                    { 9, 2 }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_Cards_CardId",
                table: "Votes",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
