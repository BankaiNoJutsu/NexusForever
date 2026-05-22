using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database.Auth;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
{
    [DbContext(typeof(AuthContext))]
    [Migration("20260522140000_AccountDailyLoginAndStoreHistory")]
    public partial class AccountDailyLoginAndStoreHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_daily_login",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    loginDaysTotal = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    rewardsAvailable = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    lastRewardItemKey = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    premiumKeyStatus = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    secondsUntilNextKey = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    lastClaimUtc = table.Column<DateTime>(type: "datetime", nullable: true),
                    lastDayIncrementUtc = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK__account_daily_login_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "account_store_purchase_history",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    offerId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    currencyId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false, defaultValue: (ushort)0),
                    price = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    purchasedUtc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "current_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK__account_store_purchase_history_accountId__account_id",
                        column: x => x.accountId,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "account_store_purchase_history");
            migrationBuilder.DropTable(name: "account_daily_login");
        }
    }
}
