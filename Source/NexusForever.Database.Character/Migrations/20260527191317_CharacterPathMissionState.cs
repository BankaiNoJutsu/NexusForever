using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class CharacterPathMissionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_path_mission",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    pathMissionId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false, defaultValue: (ushort)0),
                    pathEpisodeId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false, defaultValue: (ushort)0),
                    state = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    completed = table.Column<byte>(type: "tinyint(1) unsigned", nullable: false, defaultValue: (byte)0),
                    progressCount = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    progressData = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    xp = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.pathMissionId });
                    table.ForeignKey(
                        name: "FK__character_path_mission_id__character_id",
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
                name: "character_path_mission");
        }
    }
}
