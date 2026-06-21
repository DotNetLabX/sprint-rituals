using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Poker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPokerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanningSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Mode = table.Column<string>(type: "TEXT", nullable: false),
                    SprintId = table.Column<int>(type: "INTEGER", nullable: true),
                    SprintName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedByPersonId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedById = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedById = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EstimationItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TicketKey = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    FinalEstimate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstimationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstimationItems_PlanningSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "PlanningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    GuestToken = table.Column<string>(type: "TEXT", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionParticipants_PlanningSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "PlanningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Votes_EstimationItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "EstimationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstimationItems_SessionId",
                table: "EstimationItems",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EstimationItems_SessionId_TicketKey",
                table: "EstimationItems",
                columns: new[] { "SessionId", "TicketKey" },
                unique: true,
                filter: "\"TicketKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningSessions_CreatedByPersonId",
                table: "PlanningSessions",
                column: "CreatedByPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningSessions_Status",
                table: "PlanningSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SessionParticipants_GuestToken",
                table: "SessionParticipants",
                column: "GuestToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionParticipants_SessionId",
                table: "SessionParticipants",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionParticipants_SessionId_PersonId",
                table: "SessionParticipants",
                columns: new[] { "SessionId", "PersonId" },
                unique: true,
                filter: "\"PersonId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Votes_ItemId_ParticipantId",
                table: "Votes",
                columns: new[] { "ItemId", "ParticipantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionParticipants");

            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "EstimationItems");

            migrationBuilder.DropTable(
                name: "PlanningSessions");
        }
    }
}
