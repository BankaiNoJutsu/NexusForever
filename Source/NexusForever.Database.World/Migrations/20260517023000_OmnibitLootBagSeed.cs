using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WorldContext))]
    [Migration("20260517023000_OmnibitLootBagSeed")]
    public partial class OmnibitLootBagSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT IGNORE INTO loot_group (`id`, `parentId`, `probability`, `minDrop`, `maxDrop`, `conditionType`, `condition`, `comment`)
                VALUES (84623001, NULL, 100, 0, 0, 0, 0, 'Bag of Omnibits - Small');
                """);

            migrationBuilder.Sql(
                """
                INSERT IGNORE INTO loot_group (`id`, `parentId`, `probability`, `minDrop`, `maxDrop`, `conditionType`, `condition`, `comment`)
                VALUES (84623002, 84623001, 50, 0, 0, 0, 0, 'Bag of Omnibits - Medium');
                """);

            migrationBuilder.Sql(
                """
                INSERT IGNORE INTO loot_group (`id`, `parentId`, `probability`, `minDrop`, `maxDrop`, `conditionType`, `condition`, `comment`)
                VALUES (84623003, 84623002, 25, 0, 0, 0, 0, 'Bag of Omnibits - Large');
                """);

            migrationBuilder.Sql(
                """
                INSERT IGNORE INTO loot_item (`id`, `type`, `staticId`, `probability`, `minCount`, `maxCount`, `comment`)
                VALUES
                    (84623001, 9, 6, 100, 5, 10, 'Bag of Omnibits - Small'),
                    (84623002, 9, 6, 100, 10, 20, 'Bag of Omnibits - Medium'),
                    (84623003, 9, 6, 100, 25, 50, 'Bag of Omnibits - Large');
                """);

            migrationBuilder.Sql(
                """
                INSERT IGNORE INTO item_loot (`id`, `lootGroupId`, `comment`)
                VALUES (84623, 84623001, 'Bag of Omnibits');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM item_loot WHERE `id` = 84623 AND `lootGroupId` IN (84623001, 84623002, 84623003);");
            migrationBuilder.Sql("DELETE FROM loot_item WHERE `id` IN (84623001, 84623002, 84623003);");
            migrationBuilder.Sql("DELETE FROM loot_group WHERE `id` = 84623003;");
            migrationBuilder.Sql("DELETE FROM loot_group WHERE `id` = 84623002;");
            migrationBuilder.Sql("DELETE FROM loot_group WHERE `id` = 84623001;");
        }
    }
}
