using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AccountLoyalty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "forceSoulbind",
                table: "account_pending_item",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "forceSoulbind",
                table: "account_inventory",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "account_cosmic_operation",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    operationKey = table.Column<string>(type: "varchar(64)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    amount = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    subtract = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    earned = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.accountId, x.operationKey });
                    table.ForeignKey(
                        name: "FK__account_cosmic_operation_accountId__account_id",
                        column: x => x.accountId,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "account_loyalty",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    earnedPoints = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    bootstrapWalletAmount = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    activeTrackId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    revision = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.accountId);
                    table.ForeignKey(
                        name: "FK__account_loyalty_accountId__account_id",
                        column: x => x.accountId,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "account_loyalty_slot",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    trackId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    position = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    rewardRowId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    claimed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    choice = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    deliveryKey = table.Column<string>(type: "varchar(128)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.accountId, x.trackId, x.position });
                    table.ForeignKey(
                        name: "FK__account_loyalty_slot_accountId__account_id",
                        column: x => x.accountId,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_cosmic_operation");

            migrationBuilder.DropTable(
                name: "account_loyalty");

            migrationBuilder.DropTable(
                name: "account_loyalty_slot");

            migrationBuilder.DropColumn(
                name: "forceSoulbind",
                table: "account_pending_item");

            migrationBuilder.DropColumn(
                name: "forceSoulbind",
                table: "account_inventory");
        }
    }
}
