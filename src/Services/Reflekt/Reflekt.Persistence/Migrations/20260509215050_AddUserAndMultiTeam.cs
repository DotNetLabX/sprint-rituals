using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflekt.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndMultiTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Users table
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    GivenName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            // 2. Add nullable OwnerId to Teams
            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Teams",
                type: "INTEGER",
                nullable: true,
                defaultValue: null);

            // 3. Seed User records from distinct CreatorEmail values in Teams
            migrationBuilder.Sql(@"
                INSERT INTO ""Users"" (""Email"", ""Name"", ""GivenName"", ""CreatedAt"")
                SELECT DISTINCT ""CreatorEmail"", ""CreatorEmail"", ""CreatorEmail"", datetime('now')
                FROM ""Teams""
                WHERE ""CreatorEmail"" IS NOT NULL AND ""CreatorEmail"" != '';
            ");

            // 4. Link Teams.OwnerId to the corresponding User
            migrationBuilder.Sql(@"
                UPDATE ""Teams"" SET ""OwnerId"" = (
                    SELECT ""Id"" FROM ""Users""
                    WHERE ""Users"".""Email"" = ""Teams"".""CreatorEmail"" COLLATE NOCASE
                )
                WHERE ""CreatorEmail"" IS NOT NULL AND ""CreatorEmail"" != '';
            ");

            // 5. SQLite does not support ALTER COLUMN — must recreate Teams table to:
            //    - Make OwnerId NOT NULL
            //    - Drop CreatorEmail
            //    - Add composite unique index and FK
            migrationBuilder.Sql(@"
                CREATE TABLE ""Teams_new"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Teams"" PRIMARY KEY AUTOINCREMENT,
                    ""Name"" TEXT NOT NULL,
                    ""OwnerId"" INTEGER NOT NULL,
                    ""CreatedAt"" TEXT NOT NULL,
                    CONSTRAINT ""FK_Teams_Users_OwnerId"" FOREIGN KEY (""OwnerId"") REFERENCES ""Users"" (""Id"") ON DELETE RESTRICT
                );

                INSERT INTO ""Teams_new"" (""Id"", ""Name"", ""OwnerId"", ""CreatedAt"")
                SELECT ""Id"", ""Name"", ""OwnerId"", ""CreatedAt""
                FROM ""Teams"";

                DROP TABLE ""Teams"";

                ALTER TABLE ""Teams_new"" RENAME TO ""Teams"";
            ");

            // 6. Recreate indexes for Teams
            migrationBuilder.CreateIndex(
                name: "IX_Teams_OwnerId_Name",
                table: "Teams",
                columns: new[] { "OwnerId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // NOTE: This is a destructive data migration — original CreatorEmail values cannot be
            // recovered. CreatorEmail is restored as an empty string placeholder. This is intentional.

            // 1. Drop the composite unique index on Teams (defined outside the table body)
            migrationBuilder.DropIndex(
                name: "IX_Teams_OwnerId_Name",
                table: "Teams");

            // 2. SQLite does not support DROP COLUMN or DROP FOREIGN KEY — rebuild Teams table
            //    without OwnerId and with CreatorEmail restored. This must happen BEFORE dropping
            //    Users to satisfy the FK constraint (Teams references Users).
            migrationBuilder.Sql(@"
                CREATE TABLE ""Teams_old"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Teams"" PRIMARY KEY AUTOINCREMENT,
                    ""Name"" TEXT NOT NULL,
                    ""CreatorEmail"" TEXT NOT NULL DEFAULT '',
                    ""CreatedAt"" TEXT NOT NULL
                );

                INSERT INTO ""Teams_old"" (""Id"", ""Name"", ""CreatorEmail"", ""CreatedAt"")
                SELECT ""Id"", ""Name"", '', ""CreatedAt""
                FROM ""Teams"";

                DROP TABLE ""Teams"";

                ALTER TABLE ""Teams_old"" RENAME TO ""Teams"";
            ");

            // 3. Restore unique index on CreatorEmail
            migrationBuilder.CreateIndex(
                name: "IX_Teams_CreatorEmail",
                table: "Teams",
                column: "CreatorEmail",
                unique: true);

            // 4. Drop Users table — safe now that Teams no longer references it
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
