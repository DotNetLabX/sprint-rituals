using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fokus.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBugRatioAlertSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BugRatioAlertThreshold",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<int>(
                name: "BugRatioConsecutiveSprintCount",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BugRatioAlertThreshold",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "BugRatioConsecutiveSprintCount",
                table: "AppSettings");
        }
    }
}
