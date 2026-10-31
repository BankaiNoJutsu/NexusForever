using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class ContractCompletionReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_contract_completion",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    acceptanceId = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    questId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false),
                    trackId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    basePoints = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    points = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    payloadVersion = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    payloadJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    createdUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.acceptanceId });
                    table.ForeignKey(
                        name: "FK_contract_completion_character",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "character_contract_reward_track",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    trackId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    cycleId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    points = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    revision = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    active = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.trackId });
                    table.ForeignKey(
                        name: "FK_contract_reward_track_character",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_contract_completion");

            migrationBuilder.DropTable(
                name: "character_contract_reward_track");
        }
    }
}
