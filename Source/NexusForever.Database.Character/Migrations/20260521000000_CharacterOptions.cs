using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class CharacterOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "castingOptions",
                table: "character",
                type: "tinyint(3) unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<ushort>(
                name: "combatLogDisableFlags",
                table: "character",
                type: "smallint(5) unsigned",
                nullable: false,
                defaultValue: (ushort)0);

            migrationBuilder.AddColumn<bool>(
                name: "disableOtherPlayersCombatLogs",
                table: "character",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "sharedChallengeEnabled",
                table: "character",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "castingOptions",
                table: "character");

            migrationBuilder.DropColumn(
                name: "combatLogDisableFlags",
                table: "character");

            migrationBuilder.DropColumn(
                name: "disableOtherPlayersCombatLogs",
                table: "character");

            migrationBuilder.DropColumn(
                name: "sharedChallengeEnabled",
                table: "character");
        }
    }
}
