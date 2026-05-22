using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    public partial class CharacterChallenges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_challenge",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    challengeId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false, defaultValue: (ushort)0),
                    activated = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    onCooldown = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    leftArea = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    currentCount = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    currentTier = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    lastRewardTier = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    completionCount = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    activeTimerSeconds = table.Column<double>(type: "double", nullable: false, defaultValue: 0d),
                    cooldownTimerSeconds = table.Column<double>(type: "double", nullable: false, defaultValue: 0d),
                    areaFailTimerSeconds = table.Column<double>(type: "double", nullable: false, defaultValue: 0d)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.challengeId });
                    table.ForeignKey(
                        name: "FK__character_challenge_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "character_challenge");
        }
    }
}
