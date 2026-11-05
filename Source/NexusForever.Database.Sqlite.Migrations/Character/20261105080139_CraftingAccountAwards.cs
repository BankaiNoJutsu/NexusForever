using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Character
{
    /// <inheritdoc />
    public partial class CraftingAccountAwards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_crafting_account_outbox",
                columns: table => new
                {
                    id = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false),
                    characterId = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    accountId = table.Column<uint>(type: "int(10)", nullable: false),
                    realmId = table.Column<ushort>(type: "smallint(5)", nullable: false),
                    payloadJson = table.Column<string>(type: "longtext", nullable: false),
                    delivered = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    cancelled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crafting_outbox_account",
                table: "character_crafting_account_outbox",
                columns: new[] { "accountId", "realmId", "delivered" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_crafting_account_outbox");
        }
    }
}
