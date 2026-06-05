using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fokus.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQaHealthSettingsAndParentTicketKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentTicketKey",
                table: "Tickets",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QaHealthThresholds",
                table: "AppSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "QualityHealthWeight",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<string>(
                name: "QualitySubScoreWeights",
                table: "AppSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentTicketKey",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "QaHealthThresholds",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "QualityHealthWeight",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "QualitySubScoreWeights",
                table: "AppSettings");
        }
    }
}
