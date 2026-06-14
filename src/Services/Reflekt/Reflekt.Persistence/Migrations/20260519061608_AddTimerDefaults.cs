using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflekt.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimerDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscussingTimerDefault",
                table: "Sprints",
                type: "INTEGER",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "VotingTimerDefault",
                table: "Sprints",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "WritingTimerDefault",
                table: "Sprints",
                type: "INTEGER",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscussingTimerDefault",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "VotingTimerDefault",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "WritingTimerDefault",
                table: "Sprints");
        }
    }
}
