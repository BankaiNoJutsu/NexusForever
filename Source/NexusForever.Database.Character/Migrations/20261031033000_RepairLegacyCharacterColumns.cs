using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    // The old CharacterOptions and ResidenceDecorState classes have no migration
    // registration metadata. Do not register them retroactively: deployments may
    // already have these columns through schema import or EnsureCreated.
    [DbContext(typeof(CharacterContext))]
    [Migration("20261031033000_RepairLegacyCharacterColumns")]
    public sealed class RepairLegacyCharacterColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddMissing(migrationBuilder, "character", "castingOptions", "tinyint(3) unsigned");
            AddMissing(migrationBuilder, "character", "combatLogDisableFlags", "smallint(5) unsigned");
            AddMissing(migrationBuilder, "character", "disableOtherPlayersCombatLogs", "tinyint(1)");
            AddMissing(migrationBuilder, "character", "sharedChallengeEnabled", "tinyint(1)");
            AddMissing(migrationBuilder, "residence_decor", "activePropUnitId", "int(10) unsigned");
            AddMissing(migrationBuilder, "residence_decor", "decorData", "int(10) unsigned");
            AddMissing(migrationBuilder, "residence_decor", "hookBagIndex", "int(10) unsigned");
            AddMissing(migrationBuilder, "residence_decor", "hookIndex", "int(10) unsigned");
        }

        private static void AddMissing(MigrationBuilder migrationBuilder, string table, string column, string type)
        {
            // Same provider-supported conditional DDL pattern as CostumeMessageChanges.
            // A present column and all its values are left untouched.
            migrationBuilder.Sql($"""
                SET @nexus_legacy_column_sql = (
                    SELECT IF(EXISTS (
                        SELECT 1 FROM information_schema.COLUMNS
                        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}'
                    ), 'SELECT 1', 'ALTER TABLE `{table}` ADD COLUMN `{column}` {type} NOT NULL DEFAULT 0')
                );
                PREPARE nexus_legacy_column_statement FROM @nexus_legacy_column_sql;
                EXECUTE nexus_legacy_column_statement;
                DEALLOCATE PREPARE nexus_legacy_column_statement;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Columns may predate this repair and contain player settings/decor
            // state. Rollback cannot prove ownership, so it must preserve them.
        }
    }
}
