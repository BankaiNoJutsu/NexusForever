using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database.Auth;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AuthContext))]
    [Migration("20260522120000_AccountPendingItem")]
    public partial class AccountPendingItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_pending_item",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    pendingItemId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    groupName = table.Column<string>(type: "varchar(128)", nullable: false, defaultValue: ""),
                    accountItemId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    senderAccountId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    senderRealmId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false, defaultValue: (ushort)0),
                    senderCharacterId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    targetRealmId = table.Column<ushort>(type: "smallint(5) unsigned", nullable: false, defaultValue: (ushort)0),
                    targetCharacterId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    claimState = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    unknown1 = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    createTime = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "current_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.pendingItemId });
                    table.ForeignKey(
                        name: "FK__account_pending_item_id__account_id",
                        column: x => x.id,
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
                name: "account_pending_item");
        }
    }
}
