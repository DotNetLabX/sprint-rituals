using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflekt.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GroupRetroSessionColumnsAndTimersIntoVOs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WritingTimerDefault",
                table: "RetroSessions",
                newName: "Timers_Writing");

            migrationBuilder.RenameColumn(
                name: "VotingTimerDefault",
                table: "RetroSessions",
                newName: "Timers_Voting");

            migrationBuilder.RenameColumn(
                name: "DiscussingTimerDefault",
                table: "RetroSessions",
                newName: "Timers_Discussing");

            migrationBuilder.RenameColumn(
                name: "Column3Name",
                table: "RetroSessions",
                newName: "Column3_Name");

            migrationBuilder.RenameColumn(
                name: "Column3Color",
                table: "RetroSessions",
                newName: "Column3_Color");

            migrationBuilder.RenameColumn(
                name: "Column2Name",
                table: "RetroSessions",
                newName: "Column2_Name");

            migrationBuilder.RenameColumn(
                name: "Column2Color",
                table: "RetroSessions",
                newName: "Column2_Color");

            migrationBuilder.RenameColumn(
                name: "Column1Name",
                table: "RetroSessions",
                newName: "Column1_Name");

            migrationBuilder.RenameColumn(
                name: "Column1Color",
                table: "RetroSessions",
                newName: "Column1_Color");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timers_Writing",
                table: "RetroSessions",
                newName: "WritingTimerDefault");

            migrationBuilder.RenameColumn(
                name: "Timers_Voting",
                table: "RetroSessions",
                newName: "VotingTimerDefault");

            migrationBuilder.RenameColumn(
                name: "Timers_Discussing",
                table: "RetroSessions",
                newName: "DiscussingTimerDefault");

            migrationBuilder.RenameColumn(
                name: "Column3_Name",
                table: "RetroSessions",
                newName: "Column3Name");

            migrationBuilder.RenameColumn(
                name: "Column3_Color",
                table: "RetroSessions",
                newName: "Column3Color");

            migrationBuilder.RenameColumn(
                name: "Column2_Name",
                table: "RetroSessions",
                newName: "Column2Name");

            migrationBuilder.RenameColumn(
                name: "Column2_Color",
                table: "RetroSessions",
                newName: "Column2Color");

            migrationBuilder.RenameColumn(
                name: "Column1_Name",
                table: "RetroSessions",
                newName: "Column1Name");

            migrationBuilder.RenameColumn(
                name: "Column1_Color",
                table: "RetroSessions",
                newName: "Column1Color");
        }
    }
}
