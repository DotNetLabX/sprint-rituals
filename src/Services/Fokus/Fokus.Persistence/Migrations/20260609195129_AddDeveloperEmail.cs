using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fokus.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeveloperEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Developers",
                type: "TEXT",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Developers");
        }
    }
}
