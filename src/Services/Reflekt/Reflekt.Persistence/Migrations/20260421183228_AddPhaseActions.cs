using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Reflekt.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhaseActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhaseActions",
                columns: table => new
                {
                    Phase = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhaseActions", x => new { x.Phase, x.Action });
                });

            migrationBuilder.InsertData(
                table: "PhaseActions",
                columns: new[] { "Action", "Phase" },
                values: new object[,]
                {
                    { 0, 0 },
                    { 1, 0 },
                    { 2, 0 },
                    { 8, 0 },
                    { 9, 0 },
                    { 10, 0 },
                    { 6, 1 },
                    { 7, 1 },
                    { 10, 1 },
                    { 3, 2 },
                    { 4, 2 },
                    { 5, 2 },
                    { 8, 2 },
                    { 9, 2 },
                    { 10, 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhaseActions");
        }
    }
}
