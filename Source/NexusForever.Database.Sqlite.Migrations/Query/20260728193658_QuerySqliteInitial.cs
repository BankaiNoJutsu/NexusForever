using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Query
{
    /// <inheritdoc />
    public partial class QuerySqliteInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character",
                columns: table => new
                {
                    characterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    realmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: true),
                    realmName = table.Column<string>(type: "TEXT", nullable: true),
                    race = table.Column<byte>(type: "INTEGER", nullable: false),
                    @class = table.Column<byte>(name: "class", type: "INTEGER", nullable: false),
                    path = table.Column<byte>(type: "INTEGER", nullable: false),
                    faction = table.Column<uint>(type: "INTEGER", nullable: false),
                    sex = table.Column<byte>(type: "INTEGER", nullable: false),
                    currentRealmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    worldZoneId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    level = table.Column<uint>(type: "INTEGER", nullable: false),
                    guildName = table.Column<string>(type: "TEXT", nullable: true),
                    lastOnline = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character", x => new { x.characterId, x.realmId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_character_class",
                table: "character",
                column: "class");

            migrationBuilder.CreateIndex(
                name: "IX_character_currentRealmId",
                table: "character",
                column: "currentRealmId");

            migrationBuilder.CreateIndex(
                name: "IX_character_guildName",
                table: "character",
                column: "guildName");

            migrationBuilder.CreateIndex(
                name: "IX_character_level",
                table: "character",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "IX_character_name",
                table: "character",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_character_path",
                table: "character",
                column: "path");

            migrationBuilder.CreateIndex(
                name: "IX_character_race",
                table: "character",
                column: "race");

            migrationBuilder.CreateIndex(
                name: "IX_character_worldZoneId",
                table: "character",
                column: "worldZoneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character");
        }
    }
}
