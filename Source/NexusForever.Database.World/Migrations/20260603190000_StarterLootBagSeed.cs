using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WorldContext))]
    [Migration("20260603190000_StarterLootBagSeed")]
    public partial class StarterLootBagSeed : Migration
    {
        private const ulong ExileRucksackMountGroupId = 80875002ul;
        private const ulong DominionRucksackMountGroupId = 80875003ul;
        private const ulong SurvivalKitGroupId = 83615001ul;

        private static readonly RucksackSeed[] RucksackSeeds =
        [
            new(80875u, 80875001ul, 80875010ul, 80876u, Range(82720u, 16u), "1", "I"),
            new(80876u, 80876001ul, 80876010ul, 80877u, Range(82736u, 64u), "2", "II"),
            new(80877u, 80877001ul, 80877010ul, 80878u, Range(82800u, 64u), "3", "III"),
            new(80878u, 80878001ul, 80878010ul, 80879u, Range(82864u, 64u), "4", "IV"),
            new(80879u, 80879001ul, 80879010ul, 80880u, Range(82928u, 64u), "5", "V"),
            new(80880u, 80880001ul, 80880010ul, 80881u, Range(82992u, 64u), "6", "VI"),
            new(80881u, 80881001ul, 80881010ul, 80882u, Range(83056u, 64u), "7", "VII"),
            new(80882u, 80882001ul, 80882010ul, 80883u, Range(83120u, 64u), "8", "VIII"),
            new(80883u, 80883001ul, 80883010ul, 80884u, Range(83184u, 64u), "9", "IX"),
            new(80884u, 80884001ul, 80884010ul, null, Range(83248u, 64u), "10", "X")
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
                DELETE FROM loot_item WHERE `id` = 80875001 AND `type` = 2 AND `staticId` = 1;

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
            migrationBuilder.Sql($"DELETE FROM loot_item WHERE `id` IN ({BuildLootItemGroupIds()});");
            migrationBuilder.Sql($"DELETE FROM loot_group WHERE `id` IN ({BuildChildLootGroupIds()});");
            migrationBuilder.Sql($"DELETE FROM loot_group WHERE `id` IN ({BuildRootLootGroupIds()});");
        }

        private static string BuildLootGroupRows()
        {
            List<string> rows = [];
            foreach (RucksackSeed seed in RucksackSeeds)
            {
                rows.Add($"    ({seed.RootGroupId}, NULL, 100, 0, 0, 0, 0, 'Protostar Revolutionary Rucksack {seed.Number} - root')");
                rows.Add($"    ({seed.GearGroupId}, {seed.RootGroupId}, 100, 1, 1, 0, 0, 'Protostar Revolutionary Rucksack {seed.Number} - WIP inferred PvP gear Mk {seed.Mark}')");
            }

            rows.Add($"    ({ExileRucksackMountGroupId}, 80875001, 100, 0, 0, 9, 167, 'Protostar Revolutionary Rucksack 1 - Exile provisionary mount license')");
            rows.Add($"    ({DominionRucksackMountGroupId}, 80875001, 100, 0, 0, 9, 166, 'Protostar Revolutionary Rucksack 1 - Dominion provisionary mount license')");
            rows.Add($"    ({SurvivalKitGroupId}, NULL, 100, 0, 0, 0, 0, 'Nexus Survival Kit')");
            return string.Join(",\n", rows);
        }

        private static string BuildLootItemRows()
        {
            List<string> rows = [];
            foreach (RucksackSeed seed in RucksackSeeds)
            {
                if (seed.NextRucksackItemId.HasValue)
                    rows.Add($"    ({seed.RootGroupId}, 0, {seed.NextRucksackItemId.Value}, 100, 1, 1, 'Protostar Revolutionary Rucksack {seed.Number} - next rucksack')");

                foreach (uint gearItemId in seed.GearItemIds)
                    rows.Add($"    ({seed.GearGroupId}, 0, {gearItemId}, 100, 1, 1, 'Protostar Revolutionary Rucksack {seed.Number} - WIP inferred PvP gear Mk {seed.Mark}')");
            }

            rows.Add($"    ({ExileRucksackMountGroupId}, 0, 49979, 100, 1, 1, 'Protostar Revolutionary Rucksack 1 - Equivar (Provisionary) License')");
            rows.Add($"    ({DominionRucksackMountGroupId}, 0, 49854, 100, 1, 1, 'Protostar Revolutionary Rucksack 1 - Velocirex (Provisionary) License')");
            rows.Add($"    ({SurvivalKitGroupId}, 0, 81911, 100, 5, 5, 'Nexus Survival Kit - Salted Steak')");
            rows.Add($"    ({SurvivalKitGroupId}, 0, 81921, 100, 5, 5, 'Nexus Survival Kit - VendiShot Mk. V')");
            rows.Add($"    ({SurvivalKitGroupId}, 0, 14892, 100, 5, 5, 'Nexus Survival Kit - Go Juice')");
            return string.Join(",\n", rows);
        }

        private static string BuildItemLootRows()
        {
            IEnumerable<string> rucksackRows = RucksackSeeds
                .Select(seed => $"    ({seed.ItemId}, {seed.RootGroupId}, 'Protostar Revolutionary Rucksack {seed.Number}')");
            return string.Join(",\n", rucksackRows.Append($"    (83615, {SurvivalKitGroupId}, 'Nexus Survival Kit')"));
        }

        private static string BuildItemLootDeletePairs()
        {
            IEnumerable<string> rucksackPairs = RucksackSeeds.Select(seed => $"({seed.ItemId}, {seed.RootGroupId})");
            return string.Join(", ", rucksackPairs.Append($"(83615, {SurvivalKitGroupId})"));
        }

        private static string BuildLootItemGroupIds()
        {
            IEnumerable<ulong> ids = RucksackSeeds
                .SelectMany(seed => new[] { seed.RootGroupId, seed.GearGroupId })
                .Concat([ExileRucksackMountGroupId, DominionRucksackMountGroupId, SurvivalKitGroupId]);
            return string.Join(", ", ids);
        }

        private static string BuildChildLootGroupIds()
        {
            IEnumerable<ulong> ids = RucksackSeeds
                .Select(seed => seed.GearGroupId)
                .Concat([ExileRucksackMountGroupId, DominionRucksackMountGroupId]);
            return string.Join(", ", ids);
        }

        private static string BuildRootLootGroupIds()
        {
            IEnumerable<ulong> ids = RucksackSeeds.Select(seed => seed.RootGroupId).Append(SurvivalKitGroupId);
            return string.Join(", ", ids);
        }

        private static uint[] Range(uint start, uint count)
        {
            uint[] result = new uint[(int)count];
            for (uint i = 0u; i < count; i++)
                result[i] = start + i;

            return result;
        }

        private sealed record RucksackSeed(uint ItemId, ulong RootGroupId, ulong GearGroupId, uint? NextRucksackItemId, uint[] GearItemIds, string Number, string Mark);
    }
}
