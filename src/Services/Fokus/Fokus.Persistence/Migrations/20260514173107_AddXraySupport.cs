using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fokus.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddXraySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "XrayClientId",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XrayClientSecret",
                table: "AppSettings",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "XrayEnabled",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TestExecutions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IssueKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    AssigneeId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestExecutions_Developers_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Developers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TestSets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IssueKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    AssigneeId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestSets_Developers_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Developers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TestExecutionLinks",
                columns: table => new
                {
                    TestExecutionIssueId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TicketKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LinkType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestExecutionLinks", x => new { x.TestExecutionIssueId, x.TicketKey });
                    table.ForeignKey(
                        name: "FK_TestExecutionLinks_TestExecutions_TestExecutionIssueId",
                        column: x => x.TestExecutionIssueId,
                        principalTable: "TestExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestExecutionLinks_Tickets_TicketKey",
                        column: x => x.TicketKey,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestRuns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    TestExecutionIssueId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    StatusName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExecutedById = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestRuns_Developers_ExecutedById",
                        column: x => x.ExecutedById,
                        principalTable: "Developers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TestRuns_TestExecutions_TestExecutionIssueId",
                        column: x => x.TestExecutionIssueId,
                        principalTable: "TestExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestExecutionLink_TicketKey",
                table: "TestExecutionLinks",
                column: "TicketKey");

            migrationBuilder.CreateIndex(
                name: "IX_TestExecution_AssigneeId",
                table: "TestExecutions",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_TestRun_ExecutedById",
                table: "TestRuns",
                column: "ExecutedById");

            migrationBuilder.CreateIndex(
                name: "IX_TestRun_TestExecutionIssueId",
                table: "TestRuns",
                column: "TestExecutionIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_TestSet_AssigneeId",
                table: "TestSets",
                column: "AssigneeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestExecutionLinks");

            migrationBuilder.DropTable(
                name: "TestRuns");

            migrationBuilder.DropTable(
                name: "TestSets");

            migrationBuilder.DropTable(
                name: "TestExecutions");

            migrationBuilder.DropColumn(
                name: "XrayClientId",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "XrayClientSecret",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "XrayEnabled",
                table: "AppSettings");
        }
    }
}
