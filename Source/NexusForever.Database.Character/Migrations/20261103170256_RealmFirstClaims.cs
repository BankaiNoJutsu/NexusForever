using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class RealmFirstClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "realm_first_achievement",
                columns: table => new
                {
                    isGuild = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    achievementId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false),
                    ownerId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: true),
                    token = table.Column<string>(type: "char(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.isGuild, x.achievementId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "realm_first_achievement");
        }
    }
}
