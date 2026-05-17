using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WorldContext))]
    [Migration("20260517013000_LootTables")]
    public partial class LootTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS loot_group (
                    `id` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `parentId` bigint(20) unsigned NULL,
                    `probability` float NOT NULL DEFAULT 100,
                    `minDrop` int(10) unsigned NOT NULL DEFAULT 0,
                    `maxDrop` int(10) unsigned NOT NULL DEFAULT 0,
                    `conditionType` int(10) unsigned NOT NULL DEFAULT 0,
                    `condition` int(10) unsigned NOT NULL DEFAULT 0,
                    `comment` varchar(200) NULL DEFAULT '',
                    PRIMARY KEY (`id`),
                    KEY `IX_loot_group_parentId` (`parentId`),
                    CONSTRAINT `FK__loot_group_parentId__loot_group_id`
                        FOREIGN KEY (`parentId`) REFERENCES loot_group (`id`)
                        ON DELETE RESTRICT
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS entity_loot (
                    `id` int(10) unsigned NOT NULL DEFAULT 0,
                    `lootGroupId` bigint(20) unsigned NOT NULL,
                    `comment` varchar(200) NULL DEFAULT '',
                    PRIMARY KEY (`id`, `lootGroupId`),
                    KEY `IX_entity_loot_lootGroupId` (`lootGroupId`),
                    CONSTRAINT `FK_entity_loot_loot_group_lootGroupId`
                        FOREIGN KEY (`lootGroupId`) REFERENCES loot_group (`id`)
                        ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS item_loot (
                    `id` int(10) unsigned NOT NULL DEFAULT 0,
                    `lootGroupId` bigint(20) unsigned NOT NULL,
                    `comment` varchar(200) NULL DEFAULT '',
                    PRIMARY KEY (`id`, `lootGroupId`),
                    KEY `IX_item_loot_lootGroupId` (`lootGroupId`),
                    CONSTRAINT `FK_item_loot_loot_group_lootGroupId`
                        FOREIGN KEY (`lootGroupId`) REFERENCES loot_group (`id`)
                        ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS loot_item (
                    `id` bigint(20) unsigned NOT NULL DEFAULT 0,
                    `type` int(10) unsigned NOT NULL DEFAULT 0,
                    `staticId` int(10) unsigned NOT NULL DEFAULT 0,
                    `probability` float NOT NULL DEFAULT 100,
                    `minCount` int(10) unsigned NOT NULL DEFAULT 0,
                    `maxCount` int(10) unsigned NOT NULL DEFAULT 0,
                    `comment` varchar(200) NULL DEFAULT '',
                    PRIMARY KEY (`id`, `type`, `staticId`),
                    CONSTRAINT `FK__loot_item_id__loot_group_id`
                        FOREIGN KEY (`id`) REFERENCES loot_group (`id`)
                        ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS creature_loot (
                    `creatureId` int(10) unsigned NOT NULL,
                    `itemId` int(10) unsigned NOT NULL,
                    `chance` decimal(12,8) NOT NULL DEFAULT 0,
                    `dropTimes` int(10) unsigned NOT NULL DEFAULT 0,
                    `aggregateDropSum` int(10) unsigned NOT NULL DEFAULT 0,
                    `aggregateDropCount` int(10) unsigned NOT NULL DEFAULT 0,
                    `gameVersion` int(10) unsigned NOT NULL DEFAULT 0,
                    `sourceDropId` int(10) unsigned NOT NULL DEFAULT 0,
                    `versionedItemDropAggregateId` int(10) unsigned NOT NULL DEFAULT 0,
                    `versionedCreatureDropAggregateId` int(10) unsigned NOT NULL DEFAULT 0,
                    `lastSeenIn` int(10) unsigned NOT NULL DEFAULT 0,
                    `matchStatus` varchar(32) NOT NULL DEFAULT '',
                    `sourceName` varchar(255) NOT NULL DEFAULT '',
                    `itemName` varchar(255) NOT NULL DEFAULT '',
                    PRIMARY KEY (`creatureId`, `itemId`),
                    KEY `ix_creature_loot_item` (`itemId`),
                    KEY `ix_creature_loot_chance` (`chance`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS creature_loot;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS entity_loot;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS item_loot;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS loot_item;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS loot_group;");
        }
    }
}
