using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
{
    [DbContext(typeof(AuthContext))]
    [Migration("20261029200000_AccountPendingItemSequence")]
    public partial class AccountPendingItemSequence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "nextPendingItemId",
                table: "account",
                type: "bigint(20) unsigned",
                nullable: false,
                defaultValue: 1ul);

            migrationBuilder.CreateTable(
                name: "account_pending_item_delivery",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    deliveryKey = table.Column<string>(type: "varchar(128)", nullable: false),
                    pendingItemId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", row => new { row.accountId, row.deliveryKey });
                    table.ForeignKey(name: "FK__account_pending_item_delivery_accountId__account_id",
                        column: row => row.accountId, principalTable: "account", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "account_pending_item_delivery");
            migrationBuilder.DropColumn(name: "nextPendingItemId", table: "account");
        }
    }
}
