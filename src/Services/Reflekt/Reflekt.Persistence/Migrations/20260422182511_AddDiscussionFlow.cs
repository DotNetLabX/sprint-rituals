using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Reflekt.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscussionFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FocusedGroupId",
                table: "Sprints",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscussedAt",
                table: "CardGroups",
                type: "TEXT",
                nullable: true);

            migrationBuilder.InsertData(
                table: "PhaseActions",
                columns: new[] { "Action", "Phase" },
                values: new object[,]
                {
                    { 14, 3 },
                    { 15, 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 14, 3 });

            migrationBuilder.DeleteData(
                table: "PhaseActions",
                keyColumns: new[] { "Action", "Phase" },
                keyValues: new object[] { 15, 3 });

            migrationBuilder.DropColumn(
                name: "FocusedGroupId",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "DiscussedAt",
                table: "CardGroups");
        }
    }
}
