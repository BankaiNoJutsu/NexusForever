using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
{
    /// <inheritdoc />
    public partial class CraftingAccountAwards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "nextInventoryId",
                table: "account",
                type: "bigint(20) unsigned",
                nullable: false,
                defaultValue: 1ul);

            migrationBuilder.CreateTable(
                name: "account_crafting_receipt",
                columns: table => new
                {
                    id = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    realmId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false),
                    payloadJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    delivered = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    cancelled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    completed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_crafting_receipt_account",
                table: "account_crafting_receipt",
                columns: new[] { "accountId", "realmId", "completed" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_crafting_receipt");

            migrationBuilder.DropColumn(
                name: "nextInventoryId",
                table: "account");
        }
    }
}
