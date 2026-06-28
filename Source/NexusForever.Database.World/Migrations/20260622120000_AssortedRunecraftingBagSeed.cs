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
    [Migration("20260622120000_AssortedRunecraftingBagSeed")]
    public partial class AssortedRunecraftingBagSeed : Migration
    {
        private const uint StaticItemLootType = 0u;
        private const uint AssortedRunecraftingBagItemId = 83667u;

        private const ulong RootGroupId = 83667001ul;
        private const ulong SignGroupId = 83667002ul;
        private const ulong FragmentGroupId = 83667003ul;

        private static readonly LootItemSeed[] SignSeeds =
        [
            new(29316u, 9.5794f, "Sign of Water"),
            new(29596u, 8.6449f, "Sign of Fire"),
            new(29588u, 8.4891f, "Sign of Earth"),
            new(29604u, 8.4891f, "Sign of Air"),
            new(29601u, 7.2430f, "Sign of Logic - Greater"),
            new(29317u, 6.1526f, "Sign of Water - Greater"),
            new(29605u, 5.9190f, "Sign of Air - Greater"),
            new(29589u, 5.4517f, "Sign of Earth - Greater"),
            new(29321u, 5.1402f, "Sign of Life - Greater"),
            new(29597u, 5.0623f, "Sign of Fire - Greater"),
            new(29606u, 3.8941f, "Sign of Air - Major"),
            new(29322u, 3.5826f, "Sign of Life - Major"),
            new(29594u, 3.2710f, "Sign of Fusion - Major"),
            new(29602u, 3.1153f, "Sign of Logic - Major"),
            new(29590u, 2.7259f, "Sign of Earth - Major"),
            new(29318u, 2.6480f, "Sign of Water - Major"),
            new(29598u, 2.0249f, "Sign of Fire - Major"),
            new(29591u, 1.7913f, "Sign of Earth - Eldan"),
            new(29599u, 1.4798f, "Sign of Fire - Eldan"),
            new(29587u, 1.2461f, "Sign of Life - Eldan"),
            new(29603u, 1.1682f, "Sign of Logic - Eldan"),
            new(29319u, 1.0125f, "Sign of Water - Eldan"),
            new(29595u, 0.9346f, "Sign of Fusion - Eldan"),
            new(29607u, 0.9346f, "Sign of Air - Eldan")
        ];

        private static readonly LootItemSeed[] FragmentSeeds =
        [
            new(29609u, 25.7831f, "Rune Fragment"),
            new(29611u, 21.9277f, "Intricate Rune Fragment"),
            new(29612u, 18.3133f, "Armor Rune Fragment"),
            new(29613u, 15.4217f, "Eldan Rune Fragment"),
            new(44295u, 8.1928f, "Augmented Rune Fragment"),
            new(44297u, 1.6867f, "Fragment of the Engineer"),
            new(44299u, 1.4458f, "Fragment of the Stalker"),
            new(44298u, 1.2048f, "Fragment of the Medic"),
            new(34481u, 0.9639f, "Hard Rock Fragment"),
            new(34483u, 0.9639f, "Life Giver Fragment"),
            new(44301u, 0.9639f, "Fragment of the Esper"),
            new(44296u, 0.7229f, "Fragment of the Warrior"),
            new(44300u, 0.7229f, "Fragment of the Spellslinger"),
            new(34482u, 0.4819f, "Logic Bomb Fragment"),
            new(34484u, 0.4819f, "Shadow Blast Fragment"),
            new(34478u, 0.2410f, "Crystallize Fragment"),
            new(34479u, 0.2410f, "Fire Starter Fragment"),
            new(34480u, 0.2410f, "Head Wind Fragment")
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                INSERT IGNORE INTO loot_group (`id`, `parentId`, `probability`, `minDrop`, `maxDrop`, `conditionType`, `condition`, `comment`)
                VALUES
                    ({RootGroupId}, NULL, 100, 0, 0, 0, 0, 'Assorted Runecrafting Bag - WIP derived signs/fragments root'),
                    ({SignGroupId}, {RootGroupId}, 100, 1, 1, 0, 0, 'Assorted Runecrafting Bag - WIP derived signs'),
                    ({FragmentGroupId}, {RootGroupId}, 100, 1, 1, 0, 0, 'Assorted Runecrafting Bag - WIP derived fragments');
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
                VALUES ({AssortedRunecraftingBagItemId}, {RootGroupId}, 'Assorted Runecrafting Bag - WIP derived from mapped runecrafting sign/fragment bags');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DELETE FROM item_loot WHERE `id` = {AssortedRunecraftingBagItemId} AND `lootGroupId` = {RootGroupId};");
            migrationBuilder.Sql($"DELETE FROM loot_item WHERE `id` IN ({SignGroupId}, {FragmentGroupId});");
            migrationBuilder.Sql($"DELETE FROM loot_group WHERE `id` = {FragmentGroupId};");
            migrationBuilder.Sql($"DELETE FROM loot_group WHERE `id` = {SignGroupId};");
            migrationBuilder.Sql($"DELETE FROM loot_group WHERE `id` = {RootGroupId};");
        }

        private static string BuildLootItemRows()
        {
            IEnumerable<string> signRows = SignSeeds.Select(seed => BuildLootItemRow(SignGroupId, seed, "sign"));
            IEnumerable<string> fragmentRows = FragmentSeeds.Select(seed => BuildLootItemRow(FragmentGroupId, seed, "fragment"));
            return string.Join(",\n", signRows.Concat(fragmentRows));
        }

        private static string BuildLootItemRow(ulong groupId, LootItemSeed seed, string pool)
        {
            return $"    ({groupId}, {StaticItemLootType}, {seed.ItemId}, {seed.Probability.ToString("R", CultureInfo.InvariantCulture)}, 1, 1, 'Assorted Runecrafting Bag - WIP {pool}: {seed.Name}')";
        }

        private sealed record LootItemSeed(uint ItemId, float Probability, string Name);
    }
}
