using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Entity
{
    public static class ItemRuneInstallValidator
    {
        public static TradeskillResult ValidateInstallTargets(IGameTableManager gameTableManager, IItem item, IReadOnlyList<uint> runeItem2Ids)
        {
            ItemRuneSlotInitializer.ApplyDefaultSockets(item, gameTableManager);

            if (runeItem2Ids.Count > item.RuneSlots.Count)
                return TradeskillResult.InvalidSlot;

            TradeskillResult duplicateResult = ValidateNoDuplicateInstalls(runeItem2Ids);
            if (duplicateResult != TradeskillResult.Success)
                return duplicateResult;

            for (int i = 0; i < runeItem2Ids.Count; i++)
            {
                uint runeItem2Id = runeItem2Ids[i];
                if (runeItem2Id == 0u)
                    continue;

                if (i >= item.RuneSlots.Count)
                    return TradeskillResult.InvalidSlot;

                TradeskillResult matchResult = ValidateRuneMatchesSocket(gameTableManager, item, item.RuneSlots[i].Type, runeItem2Id);
                if (matchResult != TradeskillResult.Success)
                    return matchResult;
            }

            return TradeskillResult.Success;
        }

        public static TradeskillResult ValidateRuneMatchesSocket(IGameTableManager gameTableManager, IItem gearItem, RuneType socketType, uint runeItem2Id)
        {
            Item2Entry runeItem2 = gameTableManager.Item?.GetEntry(runeItem2Id);
            if (runeItem2 == null)
                return TradeskillResult.MissingRune;

            Item2CategoryEntry category = gameTableManager.Item2Category?.GetEntry(runeItem2.Item2CategoryId);
            if (category == null || category.TradeSkillId != ItemRuneGlyphTypes.RunecraftingTradeSkillId)
                return TradeskillResult.InvalidSlot;

            if (!ItemRuneGlyphTypes.TryGetRuneTypeFromRunecraftingGlyph(runeItem2.Item2CategoryId, runeItem2.Item2TypeId, out RuneType glyphType))
                return TradeskillResult.InvalidSlot;

            uint slotBit = ItemRuneSocketTypes.MapRuneTypeToBit(socketType);
            if (slotBit == 0)
                return TradeskillResult.InvalidSlot;

            // RuneInstall_ValidateRuneMatchesSocket 14040f1b0: Fusion socket (bit 0x40) accepts any runecrafting glyph.
            if (slotBit == 0x40)
                return TradeskillResult.Success;

            if (glyphType != socketType)
                return TradeskillResult.InvalidSlot;

            uint allowedMask = ItemRuneSocketMaskBuilder.BuildAllowedSocketMask(gearItem, gameTableManager);
            if (allowedMask == 0)
                return TradeskillResult.InvalidSlot;

            return (allowedMask & slotBit) != 0 ? TradeskillResult.Success : TradeskillResult.InvalidSlot;
        }

        private static TradeskillResult ValidateNoDuplicateInstalls(IReadOnlyList<uint> runeItem2Ids)
        {
            HashSet<uint> seen = new();
            foreach (uint runeItem2Id in runeItem2Ids)
            {
                if (runeItem2Id == 0u)
                    continue;

                if (!seen.Add(runeItem2Id))
                    return TradeskillResult.DuplicateRune;
            }

            return TradeskillResult.Success;
        }
    }
}
