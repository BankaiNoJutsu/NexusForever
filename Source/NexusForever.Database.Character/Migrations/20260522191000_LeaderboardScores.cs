using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    public partial class LeaderboardScores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "leaderboard_pve_score",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    characterId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    realmId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false, defaultValue: (ushort)0),
                    type = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    matchingGameMapId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    primeLevel = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    completionTime = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    rewardedTier = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    playerName = table.Column<string>(type: "varchar(64)", nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    playerClass = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    guildId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    teamMembersJson = table.Column<string>(type: "varchar(512)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recordedUtc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "current_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "leaderboard_pvp_score",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    characterId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    realmId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false, defaultValue: (ushort)0),
                    type = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    rating = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    playerName = table.Column<string>(type: "varchar(64)", nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    playerClass = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    guildId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    teamMembersJson = table.Column<string>(type: "varchar(512)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recordedUtc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "current_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_leaderboard_pve_score_scope",
                table: "leaderboard_pve_score",
                columns: new[] { "realmId", "type", "matchingGameMapId", "primeLevel", "completionTime" });

            migrationBuilder.CreateIndex(
                name: "IX_leaderboard_pvp_score_scope",
                table: "leaderboard_pvp_score",
                columns: new[] { "realmId", "type", "rating" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "leaderboard_pve_score");
            migrationBuilder.DropTable(name: "leaderboard_pvp_score");
        }
    }
}
