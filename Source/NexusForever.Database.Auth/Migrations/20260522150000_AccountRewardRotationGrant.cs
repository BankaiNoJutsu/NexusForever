using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database.Auth;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
{
    [DbContext(typeof(AuthContext))]
    [Migration("20260522150000_AccountRewardRotationGrant")]
    public partial class AccountRewardRotationGrant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_reward_rotation_grant",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    rewardRotationIndex = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    contentId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    rewardKeyId = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    rewardType = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false, defaultValue: (byte)0),
                    grantFlags = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    grantedUtc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "current_timestamp()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.accountId, x.rewardRotationIndex, x.contentId, x.rewardKeyId, x.rewardType });
                    table.ForeignKey(
                        name: "FK__account_reward_rotation_grant_accountId__account_id",
                        column: x => x.accountId,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "account_reward_rotation_grant");
        }
    }
}
