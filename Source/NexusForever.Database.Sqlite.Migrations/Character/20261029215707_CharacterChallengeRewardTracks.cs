using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Character
{
    /// <inheritdoc />
    public partial class CharacterChallengeRewardTracks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "activeAttemptId",
                table: "character_challenge",
                type: "char(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "character_challenge_attempt",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    attemptId = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false),
                    challengeId = table.Column<ushort>(type: "smallint(5)", nullable: false),
                    trackId = table.Column<uint>(type: "int(10)", nullable: false),
                    rewardMode = table.Column<byte>(type: "tinyint(3)", nullable: false),
                    earnedMedalMask = table.Column<uint>(type: "int(10)", nullable: false),
                    terminal = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.attemptId });
                    table.ForeignKey(
                        name: "FK_challenge_attempt_character",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_challenge_reward_track",
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
                        name: "FK_challenge_reward_track_character",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_challenge_medal_receipt",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    attemptId = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false),
                    medal = table.Column<byte>(type: "tinyint(3)", nullable: false),
                    trackId = table.Column<uint>(type: "int(10)", nullable: false),
                    points = table.Column<uint>(type: "int(10)", nullable: false),
                    multiplier = table.Column<float>(type: "float", nullable: false),
                    modifierRevision = table.Column<long>(type: "bigint(20)", nullable: false),
                    createdUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.attemptId, x.medal });
                    table.ForeignKey(
                        name: "FK_challenge_medal_receipt_attempt",
                        columns: x => new { x.id, x.attemptId },
                        principalTable: "character_challenge_attempt",
                        principalColumns: new[] { "id", "attemptId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_challenge_reward_slot",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    trackId = table.Column<uint>(type: "int(10)", nullable: false),
                    cycleId = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    ordinal = table.Column<byte>(type: "tinyint(3)", nullable: false),
                    rewardRowId = table.Column<uint>(type: "int(10)", nullable: false),
                    requiredPoints = table.Column<uint>(type: "int(10)", nullable: false),
                    claimed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.trackId, x.cycleId, x.ordinal });
                    table.ForeignKey(
                        name: "FK_challenge_reward_slot_track",
                        columns: x => new { x.id, x.trackId },
                        principalTable: "character_challenge_reward_track",
                        principalColumns: new[] { "id", "trackId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_challenge_reward_claim",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    trackId = table.Column<uint>(type: "int(10)", nullable: false),
                    cycleId = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    ordinal = table.Column<byte>(type: "tinyint(3)", nullable: false),
                    rewardRowId = table.Column<uint>(type: "int(10)", nullable: false),
                    choice = table.Column<uint>(type: "int(10)", nullable: false),
                    claimId = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false),
                    deliveryVersion = table.Column<uint>(type: "int(10)", nullable: false),
                    deliveryJson = table.Column<string>(type: "longtext", nullable: false),
                    createdUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.trackId, x.cycleId, x.ordinal });
                    table.ForeignKey(
                        name: "FK_challenge_reward_claim_slot",
                        columns: x => new { x.id, x.trackId, x.cycleId, x.ordinal },
                        principalTable: "character_challenge_reward_slot",
                        principalColumns: new[] { "id", "trackId", "cycleId", "ordinal" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_character_challenge_activeAttempt",
                table: "character_challenge",
                columns: new[] { "id", "activeAttemptId" });

            migrationBuilder.CreateIndex(
                name: "UX_character_challenge_reward_claim_claimId",
                table: "character_challenge_reward_claim",
                column: "claimId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_challenge_medal_receipt");

            migrationBuilder.DropTable(
                name: "character_challenge_reward_claim");

            migrationBuilder.DropTable(
                name: "character_challenge_attempt");

            migrationBuilder.DropTable(
                name: "character_challenge_reward_slot");

            migrationBuilder.DropTable(
                name: "character_challenge_reward_track");

            migrationBuilder.DropIndex(
                name: "IX_character_challenge_activeAttempt",
                table: "character_challenge");

            migrationBuilder.DropColumn(
                name: "activeAttemptId",
                table: "character_challenge");
        }
    }
}
