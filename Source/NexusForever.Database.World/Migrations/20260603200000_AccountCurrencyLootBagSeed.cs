using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WorldContext))]
    [Migration("20260603200000_AccountCurrencyLootBagSeed")]
    public partial class AccountCurrencyLootBagSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT IGNORE INTO loot_group (`id`, `parentId`, `probability`, `minDrop`, `maxDrop`, `conditionType`, `condition`, `comment`)
                VALUES
                    (84946001, NULL, 100, 0, 0, 0, 0, 'Sack of Service Tokens'),
                    (84949001, NULL, 100, 0, 0, 0, 0, 'Hefty Sack of Service Tokens');
                """);

            migrationBuilder.Sql(
                """
                INSERT IGNORE INTO loot_item (`id`, `type`, `staticId`, `probability`, `minCount`, `maxCount`, `comment`)
                VALUES
                    (84946001, 9, 9, 100, 30, 30, 'Sack of Service Tokens - 30 Service Tokens'),
                    (84949001, 9, 9, 100, 140, 140, 'Hefty Sack of Service Tokens - 140 Service Tokens');
                """);

            migrationBuilder.Sql(
                """
                INSERT IGNORE INTO item_loot (`id`, `lootGroupId`, `comment`)
                VALUES
                    (84946, 84946001, 'Sack of Service Tokens'),
                    (84947, 84623002, 'Satchel of OmniBits'),
                    (84948, 84623001, 'Packet of OmniBits'),
                    (84949, 84949001, 'Hefty Sack of Service Tokens');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM item_loot WHERE (`id` = 84946 AND `lootGroupId` = 84946001) OR (`id` = 84947 AND `lootGroupId` = 84623002) OR (`id` = 84948 AND `lootGroupId` = 84623001) OR (`id` = 84949 AND `lootGroupId` = 84949001);");
            migrationBuilder.Sql("DELETE FROM loot_item WHERE `id` IN (84946001, 84949001) AND `type` = 9 AND `staticId` = 9;");
            migrationBuilder.Sql("DELETE FROM loot_group WHERE `id` IN (84946001, 84949001);");
        }
    }
}
