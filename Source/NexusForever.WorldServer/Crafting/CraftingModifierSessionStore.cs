using System;
using System.Collections.Generic;
using System.Linq;

using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.WorldServer.Crafting
{
    public sealed class CraftingModifierSessionStore : ICraftingModifierSessionStore
    {
        private const int MaxCraftingModifiers = 5;

        private readonly object syncRoot = new();
        private readonly Dictionary<ulong, List<CraftingModifierState>> activeModifiersByCharacterId = [];

        public bool TryAddModifier(IPlayer player, uint additiveItem2Id, uint catalystItem2Id)
        {
            if (player == null)
                return false;

            if (additiveItem2Id == 0u && catalystItem2Id == 0u)
                return false;

            lock (syncRoot)
            {
                if (!activeModifiersByCharacterId.TryGetValue(player.CharacterId, out List<CraftingModifierState> modifiers))
                {
                    modifiers = [];
                    activeModifiersByCharacterId.Add(player.CharacterId, modifiers);
                }

                if (modifiers.Count >= MaxCraftingModifiers)
                    return false;

                modifiers.Add(new CraftingModifierState(additiveItem2Id, catalystItem2Id));
            }

            return true;
        }

        public void ClearModifiers(IPlayer player)
        {
            if (player == null)
                return;

            lock (syncRoot)
                activeModifiersByCharacterId.Remove(player.CharacterId);
        }

        public bool TryBuildModifierItemCounts(
            IPlayer player,
            IGameTableManager gameTableManager,
            TradeskillSchematic2Entry schematic,
            out IReadOnlyDictionary<uint, uint> itemCounts,
            out string reason)
        {
            itemCounts = new Dictionary<uint, uint>();
            reason     = string.Empty;

            if (player == null)
            {
                reason = "no-player";
                return false;
            }

            List<CraftingModifierState> modifiers;
            lock (syncRoot)
            {
                if (!activeModifiersByCharacterId.TryGetValue(player.CharacterId, out List<CraftingModifierState> activeModifiers)
                    || activeModifiers.Count == 0)
                    return true;

                modifiers = activeModifiers.ToList();
            }

            uint maxAdditives = Math.Min(schematic.MaxAdditives, MaxCraftingModifiers);
            if (modifiers.Count > maxAdditives)
            {
                reason = $"too-many-additives:{modifiers.Count}:{maxAdditives}";
                return false;
            }

            Dictionary<uint, uint> counts = [];
            foreach (CraftingModifierState modifier in modifiers)
            {
                if (modifier.AdditiveItem2Id != 0u)
                {
                    if (!IsCraftingAdditiveForSchematic(gameTableManager, modifier.AdditiveItem2Id, schematic, out reason))
                        return false;

                    IncrementCount(counts, modifier.AdditiveItem2Id);
                }

                if (modifier.CatalystItem2Id != 0u)
                {
                    if (!IsCraftingCatalystForSchematic(gameTableManager, modifier.CatalystItem2Id, schematic, out reason))
                        return false;

                    IncrementCount(counts, modifier.CatalystItem2Id);
                }
            }

            foreach ((uint item2Id, uint count) in counts)
            {
                if (!player.Inventory.HasItemCount(item2Id, count))
                {
                    reason = $"missing-additive-item:{item2Id}:{count}";
                    return false;
                }
            }

            itemCounts = counts;
            return true;
        }

        private static bool IsCraftingAdditiveForSchematic(IGameTableManager gameTableManager, uint item2Id, TradeskillSchematic2Entry schematic, out string reason)
        {
            reason = string.Empty;

            Item2Entry item = gameTableManager.Item?.GetEntry(item2Id);
            if (item == null || item.TradeskillAdditiveId == 0u)
            {
                reason = $"invalid-additive-item:{item2Id}";
                return false;
            }

            TradeskillAdditiveEntry additive = gameTableManager.TradeskillAdditive?.GetEntry(item.TradeskillAdditiveId);
            if (additive == null)
            {
                reason = $"invalid-additive:{item.TradeskillAdditiveId}";
                return false;
            }

            if (schematic.TradeSkillId != 0u && additive.TradeSkillId != 0u && additive.TradeSkillId != schematic.TradeSkillId)
            {
                reason = $"additive-tradeskill-mismatch:{item2Id}:{additive.TradeSkillId}:{schematic.TradeSkillId}";
                return false;
            }

            return true;
        }

        private static bool IsCraftingCatalystForSchematic(IGameTableManager gameTableManager, uint item2Id, TradeskillSchematic2Entry schematic, out string reason)
        {
            reason = string.Empty;

            Item2Entry item = gameTableManager.Item?.GetEntry(item2Id);
            if (item == null || item.TradeskillCatalystId == 0u)
            {
                reason = $"invalid-catalyst-item:{item2Id}";
                return false;
            }

            TradeskillCatalystEntry catalyst = gameTableManager.TradeskillCatalyst?.GetEntry(item.TradeskillCatalystId);
            if (catalyst == null)
            {
                reason = $"invalid-catalyst:{item.TradeskillCatalystId}";
                return false;
            }

            if (schematic.TradeSkillId != 0u && catalyst.TradeSkillId != 0u && catalyst.TradeSkillId != schematic.TradeSkillId)
            {
                reason = $"catalyst-tradeskill-mismatch:{item2Id}:{catalyst.TradeSkillId}:{schematic.TradeSkillId}";
                return false;
            }

            return true;
        }

        private static void IncrementCount(Dictionary<uint, uint> counts, uint item2Id)
        {
            counts.TryGetValue(item2Id, out uint current);
            counts[item2Id] = current + 1u;
        }

        private sealed record CraftingModifierState(uint AdditiveItem2Id, uint CatalystItem2Id);
    }
}
