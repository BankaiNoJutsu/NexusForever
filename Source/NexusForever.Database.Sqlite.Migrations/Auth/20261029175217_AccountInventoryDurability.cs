using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Auth
{
    /// <inheritdoc />
    public partial class AccountInventoryDurability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_inventory_receipt",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(10)", nullable: false),
                    deliveryKey = table.Column<string>(type: "varchar(64)", nullable: false),
                    inventoryId = table.Column<ulong>(type: "bigint(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.accountId, x.deliveryKey });
                    table.ForeignKey(
                        name: "FK__account_inventory_receipt_accountId__account_id",
                        column: x => x.accountId,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_inventory_receipt");
        }
    }
}
