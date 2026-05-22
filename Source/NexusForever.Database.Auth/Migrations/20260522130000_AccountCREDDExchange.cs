using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database.Auth;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
{
    [DbContext(typeof(AuthContext))]
    [Migration("20260522130000_AccountCREDDExchange")]
    public partial class AccountCREDDExchange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_credd_order",
                columns: table => new
                {
                    orderId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    characterId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    realmId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false, defaultValue: (ushort)0),
                    creditAmount = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    isBuyOrder = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.orderId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "account_credd_history",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    createdUtc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "current_timestamp()"),
                    operation = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    isInitiator = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    identityRealmId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false, defaultValue: (ushort)0),
                    identityCharacterId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    counterpartyRealmId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false, defaultValue: (ushort)0),
                    counterpartyCharacterId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    creditAmount = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "account_credd_history");
            migrationBuilder.DropTable(name: "account_credd_order");
        }
    }
}
