using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeagueLens.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SyncType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SleeperLeagueId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastSuccessfulSyncAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncStatuses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncStatuses_Provider_SyncType_SleeperLeagueId",
                table: "SyncStatuses",
                columns: new[] { "Provider", "SyncType", "SleeperLeagueId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncStatuses");
        }
    }
}
