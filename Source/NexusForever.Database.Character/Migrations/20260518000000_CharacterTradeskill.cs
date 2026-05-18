using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class CharacterTradeskill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_tradeskill",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    tradeskillId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    tradeskillXp = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    isActive = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    propertyProficiencyFlags = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    talentPoints = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    talentTier00 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    talentTier01 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    talentTier02 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    talentTier03 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    talentTier04 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    talentTier05 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    talentTier06 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    talentTier07 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    talentTier08 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    talentTier09 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.tradeskillId });
                    table.ForeignKey(
                        name: "FK__character_tradeskill_id__character_id",
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
                name: "character_tradeskill");
        }
    }
}
