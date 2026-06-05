using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fokus.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeveloperSprintCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeveloperSprintCapacities",
                columns: table => new
                {
                    DeveloperAccountId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    SprintId = table.Column<int>(type: "INTEGER", nullable: false),
                    CapacityPercent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeveloperSprintCapacities", x => new { x.DeveloperAccountId, x.SprintId });
                    table.ForeignKey(
                        name: "FK_DeveloperSprintCapacities_Developers_DeveloperAccountId",
                        column: x => x.DeveloperAccountId,
                        principalTable: "Developers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeveloperSprintCapacities_Sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "Sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeveloperSprintCapacity_SprintId",
                table: "DeveloperSprintCapacities",
                column: "SprintId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeveloperSprintCapacities");
        }
    }
}
