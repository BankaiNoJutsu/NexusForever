using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.World.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WorldContext))]
    [Migration("20260609120000_StarterLootBagClassGearSeed")]
    public partial class StarterLootBagClassGearSeed : Migration
    {
        private const uint WarriorClassId = 1u;
        private const uint EngineerClassId = 2u;
        private const uint EsperClassId = 3u;
        private const uint MedicClassId = 4u;
        private const uint StalkerClassId = 5u;
        private const uint SpellslingerClassId = 7u;

        private const uint StaticItemLootType = 0u;
        private const uint IsClassLootConditionType = 1u;

        // Mk II-X rows are ordered as light/medium/heavy armor, two weapon sets, then universal gear slots.
        private static readonly ClassGearSeed[] ClassGearSeeds =
        [
            new(WarriorClassId, "Warrior", 12u, [30u], [42u, 48u]),
            new(EngineerClassId, "Engineer", 12u, [30u], [47u, 53u]),
            new(EsperClassId, "Esper", 12u, [0u], [45u, 51u]),
            new(MedicClassId, "Medic", 18u, [12u], [43u, 49u]),
            new(StalkerClassId, "Stalker", 18u, [12u], [46u, 52u]),
            new(SpellslingerClassId, "Spellslinger", 12u, [0u], [44u, 50u])
        ];

        // Mk I only has one weapon set plus the same universal gear slots.
        private static readonly ClassGearSeed[] StarterClassGearSeeds =
        [
            new(WarriorClassId, "Warrior", 0u, [], [0u]),
            new(EngineerClassId, "Engineer", 0u, [], [5u]),
            new(EsperClassId, "Esper", 0u, [], [3u]),
            new(MedicClassId, "Medic", 0u, [], [1u]),
            new(StalkerClassId, "Stalker", 0u, [], [4u]),
            new(SpellslingerClassId, "Spellslinger", 0u, [], [2u])
        ];

        private static readonly RucksackSeed[] RucksackSeeds =
        [
            new(80875001ul, 80875010ul, 82720u, 16u, "1", "I", true),
            new(80876001ul, 80876010ul, 82736u, 64u, "2", "II", false),
            new(80877001ul, 80877010ul, 82800u, 64u, "3", "III", false),
            new(80878001ul, 80878010ul, 82864u, 64u, "4", "IV", false),
            new(80879001ul, 80879010ul, 82928u, 64u, "5", "V", false),
            new(80880001ul, 80880010ul, 82992u, 64u, "6", "VI", false),
            new(80881001ul, 80881010ul, 83056u, 64u, "7", "VII", false),
            new(80882001ul, 80882010ul, 83120u, 64u, "8", "VIII", false),
            new(80883001ul, 80883010ul, 83184u, 64u, "9", "IX", false),
            new(80884001ul, 80884010ul, 83248u, 64u, "10", "X", false)
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM loot_item WHERE `id` IN ({BuildLegacyGearGroupIds()}, {BuildClassGearGroupIds()});");
            migrationBuilder.Sql($"DELETE FROM loot_group WHERE `id` IN ({BuildLegacyGearGroupIds()}, {BuildClassGearGroupIds()});");

            migrationBuilder.Sql(
                $"""
                INSERT IGNORE INTO loot_group (`id`, `parentId`, `probability`, `minDrop`, `maxDrop`, `conditionType`, `condition`, `comment`)
                VALUES
                {BuildClassLootGroupRows()};
                """);

            migrationBuilder.Sql(
                $"""
                INSERT IGNORE INTO loot_item (`id`, `type`, `staticId`, `probability`, `minCount`, `maxCount`, `comment`)
                VALUES
                {BuildClassLootItemRows()};
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM loot_item WHERE `id` IN ({BuildClassGearGroupIds()});");
            migrationBuilder.Sql($"DELETE FROM loot_group WHERE `id` IN ({BuildClassGearGroupIds()});");

            migrationBuilder.Sql(
                $"""
                INSERT IGNORE INTO loot_group (`id`, `parentId`, `probability`, `minDrop`, `maxDrop`, `conditionType`, `condition`, `comment`)
                VALUES
                {BuildLegacyLootGroupRows()};
                """);

            migrationBuilder.Sql(
                $"""
                INSERT IGNORE INTO loot_item (`id`, `type`, `staticId`, `probability`, `minCount`, `maxCount`, `comment`)
                VALUES
                {BuildLegacyLootItemRows()};
                """);
        }

        private static string BuildClassLootGroupRows()
        {
            List<string> rows = [];
            foreach (RucksackSeed seed in RucksackSeeds)
            {
                foreach (ClassGearSeed classSeed in GetClassGearSeeds(seed))
                {
                    ulong groupId = GetClassGearGroupId(seed, classSeed);
                    rows.Add($"    ({groupId}, {seed.RootGroupId}, 100, 1, 1, {IsClassLootConditionType}, {classSeed.ClassId}, 'Protostar Revolutionary Rucksack {seed.Number} - {classSeed.ClassName} PvP gear Mk {seed.Mark}')");
                }
            }

            return string.Join(",\n", rows);
        }

        private static string BuildClassLootItemRows()
        {
            List<string> rows = [];
            foreach (RucksackSeed seed in RucksackSeeds)
            {
                foreach (ClassGearSeed classSeed in GetClassGearSeeds(seed))
                {
                    ulong groupId = GetClassGearGroupId(seed, classSeed);
                    foreach (uint gearItemId in GetClassGearItemIds(seed, classSeed))
                        rows.Add($"    ({groupId}, {StaticItemLootType}, {gearItemId}, 100, 1, 1, 'Protostar Revolutionary Rucksack {seed.Number} - {classSeed.ClassName} PvP gear Mk {seed.Mark}')");
                }
            }

            return string.Join(",\n", rows);
        }

        private static string BuildLegacyLootGroupRows()
        {
            List<string> rows = [];
            foreach (RucksackSeed seed in RucksackSeeds)
                rows.Add($"    ({seed.LegacyGearGroupId}, {seed.RootGroupId}, 100, 1, 1, 0, 0, 'Protostar Revolutionary Rucksack {seed.Number} - WIP inferred PvP gear Mk {seed.Mark}')");

            return string.Join(",\n", rows);
        }

        private static string BuildLegacyLootItemRows()
        {
            List<string> rows = [];
            foreach (RucksackSeed seed in RucksackSeeds)
            {
                foreach (uint gearItemId in Range(seed.FirstGearItemId, seed.LegacyGearItemCount))
                    rows.Add($"    ({seed.LegacyGearGroupId}, {StaticItemLootType}, {gearItemId}, 100, 1, 1, 'Protostar Revolutionary Rucksack {seed.Number} - WIP inferred PvP gear Mk {seed.Mark}')");
            }

            return string.Join(",\n", rows);
        }

        private static string BuildLegacyGearGroupIds()
        {
            return string.Join(", ", RucksackSeeds.Select(seed => seed.LegacyGearGroupId));
        }

        private static string BuildClassGearGroupIds()
        {
            return string.Join(", ", RucksackSeeds.SelectMany(seed => GetClassGearSeeds(seed).Select(classSeed => GetClassGearGroupId(seed, classSeed))));
        }

        private static ulong GetClassGearGroupId(RucksackSeed seed, ClassGearSeed classSeed)
        {
            return seed.LegacyGearGroupId + classSeed.ClassId;
        }

        private static ClassGearSeed[] GetClassGearSeeds(RucksackSeed seed)
        {
            return seed.IsStarterWeaponOnly ? StarterClassGearSeeds : ClassGearSeeds;
        }

        private static IEnumerable<uint> GetClassGearItemIds(RucksackSeed seed, ClassGearSeed classSeed)
        {
            foreach (uint armorBlockOffset in classSeed.ArmorBlockOffsets)
            {
                foreach (uint itemId in Range(seed.FirstGearItemId + armorBlockOffset, classSeed.ArmorItemCount))
                    yield return itemId;
            }

            foreach (uint weaponOffset in classSeed.WeaponOffsets)
                yield return seed.FirstGearItemId + weaponOffset;

            uint universalGearOffset = seed.IsStarterWeaponOnly ? 6u : 54u;
            foreach (uint itemId in Range(seed.FirstGearItemId + universalGearOffset, 10u))
                yield return itemId;
        }

        private static uint[] Range(uint start, uint count)
        {
            uint[] result = new uint[(int)count];
            for (uint i = 0u; i < count; i++)
                result[i] = start + i;

            return result;
        }

        private sealed record RucksackSeed(ulong RootGroupId, ulong LegacyGearGroupId, uint FirstGearItemId, uint LegacyGearItemCount, string Number, string Mark, bool IsStarterWeaponOnly);

        private sealed record ClassGearSeed(uint ClassId, string ClassName, uint ArmorItemCount, uint[] ArmorBlockOffsets, uint[] WeaponOffsets);
    }
}
