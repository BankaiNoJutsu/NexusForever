using System;
using System.Linq;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Prerequisite
{
    /// <summary>
    /// NF proxies for client <c>TradeSkill_CheckItemTradeskillRequirement</c> (<c>1403c16e0</c>) and
    /// <c>TradeSkill_ClientHasKnownItem2Id</c> (<c>1403b91d0</c>) used by prerequisite types 173/174.
    /// </summary>
    internal static class TradeskillPrerequisiteHelper
    {
        public static bool HasKnownItem2Id(IPlayer player, IGameTableManager gameTableManager, uint item2Id)
        {
            foreach (TradeskillSchematic2Entry schematic in gameTableManager.TradeskillSchematic2.Entries)
            {
                if (!SchematicReferencesItem2(schematic, item2Id))
                    continue;

                if (player.HasLearnedSchematic(schematic.Id))
                    return true;
            }

            return false;
        }

        public static uint GetPlayerTradeskillTierRank(IPlayer player, IGameTableManager gameTableManager, TradeskillType tradeskillId)
        {
            if (!player.HasTradeskill(tradeskillId))
                return 0u;

            uint xp = player.GetTradeskillXp(tradeskillId);
            return gameTableManager.TradeskillTier.Entries
                .Where(entry => entry.TradeSkillId == (uint)tradeskillId && entry.RequiredXp <= xp)
                .Select(entry => entry.Tier)
                .DefaultIfEmpty(0u)
                .Max();
        }

        public static bool TryResolveTradeskillForItem2(IGameTableManager gameTableManager, uint item2Id, out TradeskillType tradeskillId)
        {
            foreach (TradeskillSchematic2Entry schematic in gameTableManager.TradeskillSchematic2.Entries)
            {
                if (!SchematicReferencesItem2(schematic, item2Id))
                    continue;

                tradeskillId = (TradeskillType)schematic.TradeSkillId;
                if (Enum.IsDefined(tradeskillId))
                    return true;
            }

            tradeskillId = default;
            return false;
        }

        private static bool SchematicReferencesItem2(TradeskillSchematic2Entry schematic, uint item2Id)
        {
            if (schematic.Item2IdOutput == item2Id
                || schematic.Item2IdOutputFail == item2Id
                || schematic.Item2IdOutputCrit == item2Id
                || schematic.Item2IdMaterial00 == item2Id
                || schematic.Item2IdMaterial01 == item2Id
                || schematic.Item2IdMaterial02 == item2Id
                || schematic.Item2IdMaterial03 == item2Id
                || schematic.Item2IdMaterial04 == item2Id)
            {
                return true;
            }

            return false;
        }
    }
}
