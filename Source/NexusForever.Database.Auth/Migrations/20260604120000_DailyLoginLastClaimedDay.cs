using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database.Auth;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
{
    [DbContext(typeof(AuthContext))]
    [Migration("20260604120000_DailyLoginLastClaimedDay")]
    public partial class DailyLoginLastClaimedDay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "lastClaimedLoginDay",
                table: "account_daily_login",
                type: "int(10) unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.Sql(@"
                UPDATE account_daily_login
                SET lastClaimedLoginDay =
                    CASE
                        WHEN rewardsAvailable >= loginDaysTotal THEN 0
                        ELSE loginDaysTotal - rewardsAvailable
                    END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lastClaimedLoginDay",
                table: "account_daily_login");
        }
    }
}
