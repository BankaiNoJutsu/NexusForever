using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WorldContext))]
    [Migration("20260514120000_EvilFromTheEtherWorldSchema")]
    public partial class EvilFromTheEtherWorldSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS entity_property (
                    `id` int(10) unsigned NOT NULL DEFAULT 0,
                    `property` tinyint(3) unsigned NOT NULL,
                    `value` float NOT NULL DEFAULT 0,
                    PRIMARY KEY (`id`, `property`),
                    CONSTRAINT FK__entity_property_id__entity_id
                        FOREIGN KEY (`id`) REFERENCES entity (`id`)
                        ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """);

            migrationBuilder.Sql(
                """
                SET @hasOldEntityPropertyValue = (
                    SELECT COUNT(*)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'entity_property'
                      AND column_name = 'alue'
                );
                SET @hasEntityPropertyValue = (
                    SELECT COUNT(*)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'entity_property'
                      AND column_name = 'value'
                );
                SET @repairEntityPropertyValue = IF(
                    @hasOldEntityPropertyValue > 0 AND @hasEntityPropertyValue = 0,
                    'ALTER TABLE entity_property CHANGE COLUMN `alue` `value` float NOT NULL DEFAULT 0',
                    'SELECT 1'
                );
                PREPARE repairEntityPropertyValueStatement FROM @repairEntityPropertyValue;
                EXECUTE repairEntityPropertyValueStatement;
                DEALLOCATE PREPARE repairEntityPropertyValueStatement;
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS creature_info_property (
                    `id` int(10) unsigned NOT NULL,
                    `property` tinyint(3) unsigned NOT NULL,
                    `value` float NOT NULL,
                    PRIMARY KEY (`id`, `property`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """);

            migrationBuilder.Sql(
                """
                SET @hasOldCreatureInfoPropertyValue = (
                    SELECT COUNT(*)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'creature_info_property'
                      AND column_name = 'alue'
                );
                SET @hasCreatureInfoPropertyValue = (
                    SELECT COUNT(*)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'creature_info_property'
                      AND column_name = 'value'
                );
                SET @repairCreatureInfoPropertyValue = IF(
                    @hasOldCreatureInfoPropertyValue > 0 AND @hasCreatureInfoPropertyValue = 0,
                    'ALTER TABLE creature_info_property CHANGE COLUMN `alue` `value` float NOT NULL',
                    'SELECT 1'
                );
                PREPARE repairCreatureInfoPropertyValueStatement FROM @repairCreatureInfoPropertyValue;
                EXECUTE repairCreatureInfoPropertyValueStatement;
                DEALLOCATE PREPARE repairCreatureInfoPropertyValueStatement;
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS creature_info_stat (
                    `id` int(10) unsigned NOT NULL,
                    `stat` tinyint(3) unsigned NOT NULL,
                    `value` float NOT NULL,
                    PRIMARY KEY (`id`, `stat`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE entity_property
                    MODIFY COLUMN `id` int(10) unsigned NOT NULL DEFAULT 0,
                    MODIFY COLUMN `property` tinyint(3) unsigned NOT NULL,
                    MODIFY COLUMN `value` float NOT NULL DEFAULT 0;

                ALTER TABLE creature_info_property
                    MODIFY COLUMN `id` int(10) unsigned NOT NULL,
                    MODIFY COLUMN `property` tinyint(3) unsigned NOT NULL,
                    MODIFY COLUMN `value` float NOT NULL;

                ALTER TABLE creature_info_stat
                    MODIFY COLUMN `id` int(10) unsigned NOT NULL,
                    MODIFY COLUMN `stat` tinyint(3) unsigned NOT NULL,
                    MODIFY COLUMN `value` float NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS creature_info_stat;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS creature_info_property;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS entity_property;");
        }
    }
}
