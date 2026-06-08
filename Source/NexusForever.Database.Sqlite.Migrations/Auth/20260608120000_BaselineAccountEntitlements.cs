using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NexusForever.Database.Auth;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Auth
{
    [DbContext(typeof(AuthContext))]
    [Migration("20260608120000_BaselineAccountEntitlements")]
    public partial class BaselineAccountEntitlements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO `account_entitlement` (`id`, `entitlementId`, `amount`)
                SELECT `account`.`id`, 15, 1
                FROM `account`
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM `account_entitlement`
                    WHERE `account_entitlement`.`id` = `account`.`id`
                      AND `account_entitlement`.`entitlementId` = 15
                );");

            migrationBuilder.Sql(@"
                INSERT INTO `account_entitlement` (`id`, `entitlementId`, `amount`)
                SELECT `account`.`id`, 17, 1
                FROM `account`
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM `account_entitlement`
                    WHERE `account_entitlement`.`id` = `account`.`id`
                      AND `account_entitlement`.`entitlementId` = 17
                );");

            migrationBuilder.Sql(@"
                UPDATE `account_entitlement`
                SET `amount` = 1
                WHERE `entitlementId` IN (15, 17)
                  AND `amount` = 0;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
