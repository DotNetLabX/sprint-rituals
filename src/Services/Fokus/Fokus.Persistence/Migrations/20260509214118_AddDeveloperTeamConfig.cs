using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fokus.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeveloperTeamConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultCapacityPercent",
                table: "Developers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Developers",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "Developer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultCapacityPercent",
                table: "Developers");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Developers");
        }
    }
}
