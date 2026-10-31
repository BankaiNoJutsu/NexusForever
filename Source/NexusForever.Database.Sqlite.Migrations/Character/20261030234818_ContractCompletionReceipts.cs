using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Character
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
                    id = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    acceptanceId = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false),
                    questId = table.Column<ushort>(type: "smallint(5)", nullable: false),
                    trackId = table.Column<uint>(type: "int(10)", nullable: false),
                    basePoints = table.Column<uint>(type: "int(10)", nullable: false),
                    points = table.Column<uint>(type: "int(10)", nullable: false),
                    payloadVersion = table.Column<uint>(type: "int(10)", nullable: false),
                    payloadJson = table.Column<string>(type: "longtext", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "character_contract_reward_track",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    trackId = table.Column<uint>(type: "int(10)", nullable: false),
                    cycleId = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    points = table.Column<uint>(type: "int(10)", nullable: false),
                    revision = table.Column<ulong>(type: "bigint(20)", nullable: false),
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
                });
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
