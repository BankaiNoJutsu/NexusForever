using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.World
{
    /// <inheritdoc />
    public partial class WorldEntityEmote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "entity_emote",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    emoteId = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.emoteId });
                    table.ForeignKey(
                        name: "FK__entity_emote_id__entity_id",
                        column: x => x.id,
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_entity_emote_id",
                table: "entity_emote",
                column: "id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entity_emote");
        }
    }
}
