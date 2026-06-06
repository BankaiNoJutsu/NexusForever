using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WorldContext))]
    [Migration("20260606140000_ItemSalvageTable")]
    public partial class ItemSalvageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS item_salvage (
                    `purpose` tinyint(3) unsigned NOT NULL DEFAULT 0,
                    `sourceItemId` int(10) unsigned NOT NULL DEFAULT 0,
                    `sourceItem2TypeId` int(10) unsigned NOT NULL DEFAULT 0,
                    `sourceLevel` int(10) unsigned NOT NULL DEFAULT 0,
                    `type` int(10) unsigned NOT NULL DEFAULT 0,
                    `staticId` int(10) unsigned NOT NULL DEFAULT 0,
                    `probability` float NOT NULL DEFAULT 100,
                    `minCount` int(10) unsigned NOT NULL DEFAULT 0,
                    `maxCount` int(10) unsigned NOT NULL DEFAULT 0,
                    `comment` varchar(200) NOT NULL DEFAULT '',
                    PRIMARY KEY (`purpose`, `sourceItemId`, `sourceItem2TypeId`, `sourceLevel`, `type`, `staticId`),
                    KEY `ix_item_salvage_exact_item` (`purpose`, `sourceItemId`),
                    KEY `ix_item_salvage_type_level` (`purpose`, `sourceItem2TypeId`, `sourceLevel`),
                    KEY `ix_item_salvage_static` (`type`, `staticId`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS item_salvage;");
        }
    }
}
