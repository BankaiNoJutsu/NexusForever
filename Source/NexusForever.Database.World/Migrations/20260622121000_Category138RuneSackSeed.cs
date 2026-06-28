using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WorldContext))]
    [Migration("20260622121000_Category138RuneSackSeed")]
    public partial class Category138RuneSackSeed : Migration
    {
        private const uint StaticItemLootType = 0u;

        private static readonly LootBagSeed[] Seeds =
        [
            new(74322u, 74048u, 74322001ul, "Celerity Rune Sack",
            [
                new(73932u, 7.1279f, "Celerity Rune of PvP Defense"),
                new(73931u, 16.9811f, "Celerity Rune of PvP Power"),
                new(73933u, 8.5954f, "Celerity Rune of PvP Power"),
                new(73934u, 10.4822f, "Celerity Rune of PvP Defense"),
                new(73929u, 7.9665f, "Celerity Rune of PvP Power"),
                new(73930u, 13.6268f, "Celerity Rune of PvP Defense"),
                new(73935u, 8.8050f, "Celerity Rune of PvP Power"),
                new(73926u, 10.0629f, "Celerity Rune of PvP Power"),
                new(73927u, 6.9182f, "Celerity Rune of PvP Defense"),
                new(73928u, 9.4340f, "Celerity Rune of PvP Defense")
            ]),
            new(74323u, 74049u, 74323001ul, "Stalwart Rune Sack",
            [
                new(73938u, 10.5263f, "Stalwart Rune of PvP Defense"),
                new(73942u, 3.3493f, "Stalwart Rune of PvP Defense"),
                new(73943u, 6.2201f, "Stalwart Rune of PvP Power"),
                new(73937u, 11.4833f, "Stalwart Rune of PvP Defense"),
                new(73941u, 11.0048f, "Stalwart Rune of PvP Power"),
                new(73944u, 17.2249f, "Stalwart Rune of PvP Defense"),
                new(73940u, 6.6986f, "Stalwart Rune of PvP Defense"),
                new(73936u, 7.6555f, "Stalwart Rune of PvP Power"),
                new(73939u, 9.0909f, "Stalwart Rune of PvP Power"),
                new(73945u, 16.7464f, "Stalwart Rune of PvP Power")
            ]),
            new(74324u, 74050u, 74324001ul, "Tactician Rune Sack",
            [
                new(73948u, 10.5691f, "Tactician Rune of PvP Defense"),
                new(73947u, 11.3821f, "Tactician Rune of PvP Defense"),
                new(73951u, 6.5041f, "Tactician Rune of PvP Power"),
                new(73954u, 7.3171f, "Tactician Rune of PvP Defense"),
                new(73953u, 9.7561f, "Tactician Rune of PvP Power"),
                new(73952u, 15.4472f, "Tactician Rune of PvP Defense"),
                new(73946u, 8.1301f, "Tactician Rune of PvP Power"),
                new(73955u, 12.1951f, "Tactician Rune of PvP Power"),
                new(73949u, 11.3821f, "Tactician Rune of PvP Power"),
                new(73950u, 7.3171f, "Tactician Rune of PvP Defense")
            ]),
            new(74325u, 74051u, 74325001ul, "Lethal Rune Sack",
            [
                new(73959u, 7.6087f, "Lethal Rune of PvP Power"),
                new(73962u, 7.6087f, "Lethal Rune of PvP Defense"),
                new(73965u, 8.6957f, "Lethal Rune of PvP Power"),
                new(73963u, 13.0435f, "Lethal Rune of PvP Power"),
                new(73958u, 10.3261f, "Lethal Rune of PvP Defense"),
                new(73957u, 8.6957f, "Lethal Rune of PvP Defense"),
                new(73964u, 12.5000f, "Lethal Rune of PvP Defense"),
                new(73960u, 10.8696f, "Lethal Rune of PvP Defense"),
                new(73961u, 7.6087f, "Lethal Rune of PvP Power"),
                new(73956u, 13.0435f, "Lethal Rune of PvP Power")
            ]),
            new(74326u, 74052u, 74326001ul, "Unbreakable Rune Sack",
            [
                new(73972u, 37.5000f, "Unbreakable Rune of PvP Defense"),
                new(73969u, 12.5000f, "Unbreakable Rune of PvP Power"),
                new(73971u, 16.6667f, "Unbreakable Rune of PvP Power"),
                new(73968u, 12.5000f, "Unbreakable Rune of PvP Defense"),
                new(73966u, 4.1667f, "Unbreakable Rune of PvP Power"),
                new(73975u, 8.3333f, "Unbreakable Rune of PvP Power"),
                new(73970u, 4.1667f, "Unbreakable Rune of PvP Defense"),
                new(73973u, 4.1667f, "Unbreakable Rune of PvP Power")
            ]),
            new(74327u, 74053u, 74327001ul, "Savior Rune Sack",
            [
                new(73978u, 9.2308f, "Savior Rune of PvP Defense"),
                new(73976u, 11.5385f, "Savior Rune of PvP Power"),
                new(73977u, 7.6923f, "Savior Rune of PvP Defense"),
                new(73984u, 15.3846f, "Savior Rune of PvP Defense"),
                new(73985u, 8.4615f, "Savior Rune of PvP Power"),
                new(73982u, 12.3077f, "Savior Rune of PvP Defense"),
                new(73983u, 12.3077f, "Savior Rune of PvP Power"),
                new(73979u, 6.9231f, "Savior Rune of PvP Power"),
                new(73980u, 5.3846f, "Savior Rune of PvP Defense"),
                new(73981u, 10.7692f, "Savior Rune of PvP Power")
            ])
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                INSERT IGNORE INTO loot_group (`id`, `parentId`, `probability`, `minDrop`, `maxDrop`, `conditionType`, `condition`, `comment`)
                VALUES
                {BuildLootGroupRows()};
                """);

            migrationBuilder.Sql(
                $"""
                INSERT IGNORE INTO loot_item (`id`, `type`, `staticId`, `probability`, `minCount`, `maxCount`, `comment`)
                VALUES
                {BuildLootItemRows()};
                """);

            migrationBuilder.Sql(
                $"""
                INSERT IGNORE INTO item_loot (`id`, `lootGroupId`, `comment`)
                VALUES
                {BuildItemLootRows()};
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM item_loot WHERE (`id`, `lootGroupId`) IN ({BuildItemLootDeletePairs()});");
            migrationBuilder.Sql($"DELETE FROM loot_item WHERE `id` IN ({BuildLootGroupIds()});");
            migrationBuilder.Sql($"DELETE FROM loot_group WHERE `id` IN ({BuildLootGroupIds()});");
        }

        private static string BuildLootGroupRows()
        {
            return string.Join(",\n", Seeds.Select(seed =>
                $"    ({seed.LootGroupId}, NULL, 100, 1, 1, 0, 0, '{seed.Name} - WIP copied from mapped Item2 {seed.SourceItemId}')"));
        }

        private static string BuildLootItemRows()
        {
            IEnumerable<string> rows = Seeds.SelectMany(seed =>
                seed.Items.Select(item => BuildLootItemRow(seed, item)));
            return string.Join(",\n", rows);
        }

        private static string BuildLootItemRow(LootBagSeed seed, LootItemSeed item)
        {
            return $"    ({seed.LootGroupId}, {StaticItemLootType}, {item.ItemId}, {item.Probability.ToString("0.####", CultureInfo.InvariantCulture)}, 1, 1, '{seed.Name} - WIP copied: {item.Name}')";
        }

        private static string BuildItemLootRows()
        {
            return string.Join(",\n", Seeds.Select(seed =>
                $"    ({seed.ItemId}, {seed.LootGroupId}, '{seed.Name} - WIP copied from mapped Item2 {seed.SourceItemId}')"));
        }

        private static string BuildItemLootDeletePairs()
        {
            return string.Join(", ", Seeds.Select(seed => $"({seed.ItemId}, {seed.LootGroupId})"));
        }

        private static string BuildLootGroupIds()
        {
            return string.Join(", ", Seeds.Select(seed => seed.LootGroupId));
        }

        private sealed record LootBagSeed(uint ItemId, uint SourceItemId, ulong LootGroupId, string Name, LootItemSeed[] Items);

        private sealed record LootItemSeed(uint ItemId, float Probability, string Name);
    }
}
