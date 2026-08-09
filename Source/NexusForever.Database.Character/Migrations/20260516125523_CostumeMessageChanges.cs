using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class CostumeMessageChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            RenameColumnWhenTargetMissing(migrationBuilder, "character_costume_item", "itemId", "item2Id");
            RenameColumnWhenTargetMissing(migrationBuilder, "character_costume", "mask", "visibilityMask");
            ConvertDyeDataToUnsigned(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RenameColumnWhenTargetMissing(migrationBuilder, "character_costume_item", "item2Id", "itemId");
            RenameColumnWhenTargetMissing(migrationBuilder, "character_costume", "visibilityMask", "mask");
            ConvertDyeDataToSigned(migrationBuilder);
        }

        private static void RenameColumnWhenTargetMissing(MigrationBuilder migrationBuilder, string table, string sourceColumn, string targetColumn)
        {
            migrationBuilder.Sql($"""
                SET @nexus_forever_costume_message_sql = (
                    SELECT IF(
                        EXISTS (
                            SELECT 1
                            FROM information_schema.COLUMNS
                            WHERE TABLE_SCHEMA = DATABASE()
                              AND TABLE_NAME = '{table}'
                              AND COLUMN_NAME = '{sourceColumn}'
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM information_schema.COLUMNS
                            WHERE TABLE_SCHEMA = DATABASE()
                              AND TABLE_NAME = '{table}'
                              AND COLUMN_NAME = '{targetColumn}'
                        ),
                        'ALTER TABLE `{table}` RENAME COLUMN `{sourceColumn}` TO `{targetColumn}`',
                        'SELECT 1'
                    )
                );
                PREPARE nexus_forever_costume_message_statement FROM @nexus_forever_costume_message_sql;
                EXECUTE nexus_forever_costume_message_statement;
                DEALLOCATE PREPARE nexus_forever_costume_message_statement;
                """);
        }

        private static void ConvertDyeDataToUnsigned(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @nexus_forever_costume_message_sql = (
                    SELECT IF(
                        EXISTS (
                            SELECT 1
                            FROM information_schema.COLUMNS
                            WHERE TABLE_SCHEMA = DATABASE()
                              AND TABLE_NAME = 'character_costume_item'
                              AND COLUMN_NAME = 'dyeData'
                              AND (DATA_TYPE <> 'int' OR COLUMN_TYPE NOT LIKE '%unsigned%')
                        ),
                        'ALTER TABLE `character_costume_item` MODIFY COLUMN `dyeData` int unsigned NOT NULL DEFAULT 0',
                        'SELECT 1'
                    )
                );
                PREPARE nexus_forever_costume_message_statement FROM @nexus_forever_costume_message_sql;
                EXECUTE nexus_forever_costume_message_statement;
                DEALLOCATE PREPARE nexus_forever_costume_message_statement;
                """);
        }

        private static void ConvertDyeDataToSigned(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @nexus_forever_costume_message_sql = (
                    SELECT IF(
                        EXISTS (
                            SELECT 1
                            FROM information_schema.COLUMNS
                            WHERE TABLE_SCHEMA = DATABASE()
                              AND TABLE_NAME = 'character_costume_item'
                              AND COLUMN_NAME = 'dyeData'
                              AND (DATA_TYPE <> 'int' OR COLUMN_TYPE LIKE '%unsigned%')
                        ),
                        'ALTER TABLE `character_costume_item` MODIFY COLUMN `dyeData` int NOT NULL DEFAULT 0',
                        'SELECT 1'
                    )
                );
                PREPARE nexus_forever_costume_message_statement FROM @nexus_forever_costume_message_sql;
                EXECUTE nexus_forever_costume_message_statement;
                DEALLOCATE PREPARE nexus_forever_costume_message_statement;
                """);
        }
    }
}
