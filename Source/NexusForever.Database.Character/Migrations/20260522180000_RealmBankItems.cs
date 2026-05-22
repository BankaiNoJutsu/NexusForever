using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    public partial class RealmBankItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "realm_bank_item",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    realmId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false, defaultValue: (ushort)0),
                    itemId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    bagIndex = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    stackCount = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    charges = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    durability = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    expirationTimeLeft = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    soulbound = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_realm_bank_item_account_realm",
                table: "realm_bank_item",
                columns: new[] { "accountId", "realmId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "realm_bank_item");
        }
    }
}
