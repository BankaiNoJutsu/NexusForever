using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class CharacterSchematicArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_galactic_archive",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    archiveArticleId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    unlockedFlags = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    viewedFlags = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.archiveArticleId });
                    table.ForeignKey(
                        name: "FK__character_galactic_archive_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_schematic",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    tradeskillSchematic2Id = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    discovered = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    discoveryCoordinateX = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    discoveryCoordinateY = table.Column<float>(type: "float", nullable: false, defaultValue: 0f)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.tradeskillSchematic2Id });
                    table.ForeignKey(
                        name: "FK__character_schematic_id__character_id",
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
                name: "character_galactic_archive");

            migrationBuilder.DropTable(
                name: "character_schematic");
        }
    }
}
