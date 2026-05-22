using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database.Auth;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
{
    [DbContext(typeof(AuthContext))]
    [Migration("20260522190000_AccountFortuneSession")]
    public partial class AccountFortuneSession : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_fortune_session",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    card0AccountItemId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    card1AccountItemId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    card2AccountItemId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    card0Rarity = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    card1Rarity = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    card2Rarity = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    card0Flipped = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    card1Flipped = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    card2Flipped = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    card0Granted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    card1Granted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    card2Granted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK__account_fortune_session_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "account_fortune_session");
        }
    }
}
