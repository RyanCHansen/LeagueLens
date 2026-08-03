using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeagueLens.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHomeAwayFromMatchup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matchups_LeagueMemberships_AwayLeagueMembershipId",
                table: "Matchups");

            migrationBuilder.DropForeignKey(
                name: "FK_Matchups_LeagueMemberships_HomeLeagueMembershipId",
                table: "Matchups");

            migrationBuilder.DropIndex(
                name: "IX_Matchups_AwayLeagueMembershipId",
                table: "Matchups");

            migrationBuilder.DropIndex(
                name: "IX_Matchups_HomeLeagueMembershipId",
                table: "Matchups");

            migrationBuilder.DropColumn(
                name: "AwayLeagueMembershipId",
                table: "Matchups");

            migrationBuilder.DropColumn(
                name: "AwayScore",
                table: "Matchups");

            migrationBuilder.DropColumn(
                name: "HomeLeagueMembershipId",
                table: "Matchups");

            migrationBuilder.DropColumn(
                name: "HomeScore",
                table: "Matchups");

            migrationBuilder.CreateTable(
                name: "MatchupParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeagueMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchupParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchupParticipants_LeagueMemberships_LeagueMembershipId",
                        column: x => x.LeagueMembershipId,
                        principalTable: "LeagueMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchupParticipants_Matchups_MatchupId",
                        column: x => x.MatchupId,
                        principalTable: "Matchups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchupParticipants_LeagueMembershipId",
                table: "MatchupParticipants",
                column: "LeagueMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchupParticipants_MatchupId_LeagueMembershipId",
                table: "MatchupParticipants",
                columns: new[] { "MatchupId", "LeagueMembershipId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchupParticipants");

            migrationBuilder.AddColumn<Guid>(
                name: "AwayLeagueMembershipId",
                table: "Matchups",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "AwayScore",
                table: "Matchups",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "HomeLeagueMembershipId",
                table: "Matchups",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "HomeScore",
                table: "Matchups",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Matchups_AwayLeagueMembershipId",
                table: "Matchups",
                column: "AwayLeagueMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_Matchups_HomeLeagueMembershipId",
                table: "Matchups",
                column: "HomeLeagueMembershipId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matchups_LeagueMemberships_AwayLeagueMembershipId",
                table: "Matchups",
                column: "AwayLeagueMembershipId",
                principalTable: "LeagueMemberships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Matchups_LeagueMemberships_HomeLeagueMembershipId",
                table: "Matchups",
                column: "HomeLeagueMembershipId",
                principalTable: "LeagueMemberships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
