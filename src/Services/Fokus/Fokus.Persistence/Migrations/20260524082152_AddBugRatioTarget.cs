using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fokus.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBugRatioTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BugRatioTarget",
                table: "AppSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 30m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BugRatioTarget",
                table: "AppSettings");
        }
    }
}
