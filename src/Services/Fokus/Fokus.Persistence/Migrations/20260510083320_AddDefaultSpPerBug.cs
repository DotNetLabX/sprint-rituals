using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fokus.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultSpPerBug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultSpPerBug",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.Sql("UPDATE \"AppSettings\" SET \"DefaultSpPerBug\" = 3 WHERE \"DefaultSpPerBug\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultSpPerBug",
                table: "AppSettings");
        }
    }
}
