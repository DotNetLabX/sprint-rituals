using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflekt.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurableColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Column1Color",
                table: "Sprints",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "green");

            migrationBuilder.AddColumn<string>(
                name: "Column1Name",
                table: "Sprints",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "Went Well");

            migrationBuilder.AddColumn<string>(
                name: "Column2Color",
                table: "Sprints",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "red");

            migrationBuilder.AddColumn<string>(
                name: "Column2Name",
                table: "Sprints",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "Didn't Go Well");

            migrationBuilder.AddColumn<string>(
                name: "Column3Color",
                table: "Sprints",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "amber");

            migrationBuilder.AddColumn<string>(
                name: "Column3Name",
                table: "Sprints",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "Start Doing");

            // ActionItems was stored as ordinal 2 (old DidntGoWell=1, ActionItems=2).
            // New enum: Column1=0, Column2=1, Column3=2, ActionItems=3.
            // Shift ActionItems from ordinal 2 to ordinal 3.
            migrationBuilder.Sql("UPDATE Cards SET ColumnType = 3 WHERE ColumnType = 2;");
            migrationBuilder.Sql("UPDATE CardGroups SET ColumnType = 3 WHERE ColumnType = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the ColumnType ordinal shift: ActionItems from 3 back to 2.
            migrationBuilder.Sql("UPDATE Cards SET ColumnType = 2 WHERE ColumnType = 3;");
            migrationBuilder.Sql("UPDATE CardGroups SET ColumnType = 2 WHERE ColumnType = 3;");

            migrationBuilder.DropColumn(
                name: "Column1Color",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "Column1Name",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "Column2Color",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "Column2Name",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "Column3Color",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "Column3Name",
                table: "Sprints");
        }
    }
}
