using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database.Auth;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AuthContext))]
    [Migration("20260517120000_AccountItemCooldown")]
    public partial class AccountItemCooldown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_item_cooldown",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    cooldownGroupId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "current_timestamp()"),
                    duration = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.cooldownGroupId });
                    table.ForeignKey(
                        name: "FK__account_item_cooldown_id__account_id",
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
                name: "account_item_cooldown");
        }
    }
}
