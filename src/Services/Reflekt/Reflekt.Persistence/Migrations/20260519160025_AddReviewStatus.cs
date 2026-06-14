using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflekt.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewStatus",
                table: "Cards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedInSprintId",
                table: "Cards",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_ClosedAt",
                table: "Sprints",
                column: "ClosedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ReviewedInSprintId",
                table: "Cards",
                column: "ReviewedInSprintId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Sprints_ReviewedInSprintId",
                table: "Cards",
                column: "ReviewedInSprintId",
                principalTable: "Sprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Sprints_ReviewedInSprintId",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_Sprints_ClosedAt",
                table: "Sprints");

            migrationBuilder.DropIndex(
                name: "IX_Cards_ReviewedInSprintId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ReviewedInSprintId",
                table: "Cards");
        }
    }
}
