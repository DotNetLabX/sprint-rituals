using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakePersonEmailOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Persons_Email",
                table: "Persons");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Persons",
                type: "TEXT",
                maxLength: 256,
                nullable: true,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 256,
                oldCollation: "NOCASE");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Email",
                table: "Persons",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Persons_Email",
                table: "Persons");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Persons",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 256,
                oldNullable: true,
                oldCollation: "NOCASE");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Email",
                table: "Persons",
                column: "Email",
                unique: true);
        }
    }
}
