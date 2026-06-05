using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fokus.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    BoardId = table.Column<int>(type: "INTEGER", nullable: true),
                    DoneStatuses = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowStages = table.Column<string>(type: "TEXT", nullable: false),
                    HealthThresholds = table.Column<string>(type: "TEXT", nullable: false),
                    HealthWeights = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Developers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AvatarUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    SubTeam = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Developers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BoardId = table.Column<int>(type: "INTEGER", nullable: false),
                    BoardName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedById = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedById = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    LastModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sprints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    IssueType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    StoryPoints = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    EpicKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    EpicName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    AssigneeId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CurrentStatus = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedById = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedById = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    LastModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Developers_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Developers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SprintMemberships",
                columns: table => new
                {
                    SprintId = table.Column<int>(type: "INTEGER", nullable: false),
                    TicketId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WasCommitted = table.Column<bool>(type: "INTEGER", nullable: false),
                    FinalStatus = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    StoryPoints = table.Column<decimal>(type: "decimal(8,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SprintMemberships", x => new { x.SprintId, x.TicketId });
                    table.ForeignKey(
                        name: "FK_SprintMemberships_Sprints_SprintId",
                        column: x => x.SprintId,
                        principalTable: "Sprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SprintMemberships_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StatusTransitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TicketId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ToStatus = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatusTransitions_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SprintMembership_TicketId",
                table: "SprintMemberships",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusTransition_TicketId_Timestamp",
                table: "StatusTransitions",
                columns: new[] { "TicketId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Ticket_AssigneeId",
                table: "Tickets",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Ticket_EpicKey",
                table: "Tickets",
                column: "EpicKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "SprintMemberships");

            migrationBuilder.DropTable(
                name: "StatusTransitions");

            migrationBuilder.DropTable(
                name: "Sprints");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Developers");
        }
    }
}
