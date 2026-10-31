using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Auth
{
    /// <inheritdoc />
    public partial class AccountPendingItemSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "nextPendingItemId",
                table: "account",
                type: "bigint(20)",
                nullable: false,
                defaultValue: 1ul);

            migrationBuilder.CreateTable(
                name: "account_pending_item_delivery",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(10)", nullable: false),
                    deliveryKey = table.Column<string>(type: "varchar(128)", nullable: false),
                    pendingItemId = table.Column<ulong>(type: "bigint(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.accountId, x.deliveryKey });
                    table.ForeignKey(
                        name: "FK__account_pending_item_delivery_accountId__account_id",
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
                name: "account_pending_item_delivery");

            migrationBuilder.DropColumn(
                name: "nextPendingItemId",
                table: "account");
        }
    }
}
