using System;
using System.Collections.Generic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network.Message.Handler.Spell;

namespace NexusForever.WorldServer.Network.Message.Handler.Item
{
    internal static class ItemUseHelper
    {
        private const uint TreasureItemCategoryId = 94u;
        private const uint TreasureItemTypeId     = 200u;

        public static bool TryUseItem(
            IWorldSession session,
            IItem item,
            IGameTableManager gameTableManager,
            uint targetUnitId = 0u,
            Position position = null,
            uint contextToken = 0u,
            string clientRequestSource = null,
            bool applyPendingSpellEvidenceCapture = false)
        {
            if (session?.Player == null || item?.Info == null)
                return false;

            ItemSpecialEntry itemSpecial = null;
            if (item.Info.Entry.ItemSpecialId00 > 0u)
            {
                itemSpecial = gameTableManager.ItemSpecial.GetEntry(item.Info.Entry.ItemSpecialId00);
                if (itemSpecial == null)
                    throw new InvalidPacketValueException();
            }

            if (itemSpecial?.Spell4IdOnActivate > 0u)
            {
                UseActivatedItem(
                    session,
                    item,
                    itemSpecial,
                    targetUnitId,
                    position,
                    contextToken,
                    clientRequestSource,
                    applyPendingSpellEvidenceCapture);
                return true;
            }

            return TryUseCurrencyTreasure(session.Player, item, gameTableManager);
        }

        private static void UseActivatedItem(
            IWorldSession session,
            IItem item,
            ItemSpecialEntry itemSpecial,
            uint targetUnitId,
            Position position,
            uint contextToken,
            string clientRequestSource,
            bool applyPendingSpellEvidenceCapture)
        {
            if (itemSpecial.PrerequisiteIdGeneric00 > 0)
            {
                if (!PrerequisiteEvaluation.Meets(session.Player, itemSpecial.PrerequisiteIdGeneric00, item))
                {
                    session.Player.SendGenericError(GenericError.UnlockItemFailed);
                    return;
                }
            }

            if (!CanUseItem(item))
                return;

            EnsureStackableItemCharges(item);

            var spellParameters = new SpellParameters
            {
                PrimaryTargetId     = targetUnitId,
                Position            = position,
                CancelActiveTrade   = true,
                ClientContextToken  = contextToken,
                ClientRequestSource = clientRequestSource
            };

            if (applyPendingSpellEvidenceCapture)
                ClientSpellEvidenceCaptureHelper.ApplyPendingCapture(session, spellParameters);

            CastResult castResult = session.Player.TryCastSpell(itemSpecial.Spell4IdOnActivate, spellParameters);
            if (castResult != CastResult.Ok)
                return;

            session.Player.Inventory.ItemUse(item);
        }

        private static bool TryUseCurrencyTreasure(IPlayer player, IItem item, IGameTableManager gameTableManager)
        {
            if (item.Info.Entry.Item2CategoryId != TreasureItemCategoryId || item.Info.Entry.Item2TypeId != TreasureItemTypeId)
                return false;

            if (!TryGetCurrencyGrants(item, gameTableManager, out List<(CurrencyType CurrencyType, ulong Amount)> grants))
                return true;

            if (!CanUseItem(item))
                return true;

            EnsureStackableItemCharges(item);

            if (!player.Inventory.ItemUse(item))
                return true;

            foreach ((CurrencyType currencyType, ulong amount) in grants)
                player.CurrencyManager.CurrencyAddAmount(currencyType, amount, isLoot: true);

            return true;
        }

        private static bool CanUseItem(IItem item)
        {
            if (item.Info.Entry.MaxStackCount > 1u)
                return item.StackCount > 0u;

            if (item.Info.Entry.MaxCharges > 0u)
                return item.Charges > 0u;

            return true;
        }

        private static void EnsureStackableItemCharges(IItem item)
        {
            if (item.Info.Entry.MaxStackCount <= 1u)
                return;

            if (item.Info.Entry.MaxCharges == 0u || item.StackCount == 0u || item.Charges != 0u)
                return;

            item.Charges = item.Info.Entry.MaxCharges;
        }

        private static bool TryGetCurrencyGrants(
            IItem item,
            IGameTableManager gameTableManager,
            out List<(CurrencyType CurrencyType, ulong Amount)> grants)
        {
            grants = [];

            CurrencyType[] currencyTypes = item.Info.Entry.CurrencyTypeId;
            uint[] currencyAmounts       = item.Info.Entry.CurrencyAmount;
            if (currencyTypes == null || currencyAmounts == null)
                return false;

            for (int i = 0; i < Math.Min(currencyTypes.Length, currencyAmounts.Length); i++)
            {
                CurrencyType currencyType = currencyTypes[i];
                uint amount               = currencyAmounts[i];
                if (currencyType == CurrencyType.None || amount == 0u)
                    continue;

                if (gameTableManager.CurrencyType.GetEntry((ulong)currencyType) == null)
                    throw new InvalidPacketValueException();

                grants.Add((currencyType, amount));
            }

            return grants.Count > 0;
        }
    }
}
