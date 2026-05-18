using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientRequestActionSetChangesHandler : IMessageHandler<IWorldSession, ClientRequestActionSetChanges>
    {
        private const int LimitedActionSlotCount = 8;
        private const uint LimitedActionSpellWeaponSlot = 5u;
        private const uint PvpPreparationRestrictedFlag = 0x40u;
        private const uint PvpInProgressRestrictedFlag = 0x04u;

        #region Dependency Injection

        private readonly IMatchManager matchManager;

        public ClientRequestActionSetChangesHandler(
            IMatchManager matchManager)
        {
            this.matchManager = matchManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientRequestActionSetChanges requestActionSetChanges)
        {
            LimitedActionSetResult validationResult = ValidateRequest(session, requestActionSetChanges);
            if (validationResult != LimitedActionSetResult.Ok)
            {
                SendActionSetResult(session, requestActionSetChanges.ActionSetIndex, validationResult);
                return;
            }

            IActionSet actionSet = session.Player.SpellManager.GetActionSet(requestActionSetChanges.ActionSetIndex);
            byte previousTierPoints = actionSet.TierPoints;

            List<IActionSetShortcut> spellShortcuts = actionSet.Actions
                .Where(shortcut => shortcut.ShortcutType == ShortcutType.SpellbookItem)
                .ToList();

            for (UILocation i = 0; i < (UILocation)requestActionSetChanges.Actions.Count; i++)
            {
                IActionSetShortcut shortcut = actionSet.GetShortcut(i);
                if (shortcut != null)
                    actionSet.RemoveShortcut(i);

                uint spell4BaseId = requestActionSetChanges.Actions[(int)i];
                if (spell4BaseId != 0u)
                {
                    IActionSetShortcut existingShortcut = spellShortcuts.SingleOrDefault(s => s.ObjectId == spell4BaseId);
                    byte tier = existingShortcut?.Tier ?? 1;
                    actionSet.AddShortcut(i, ShortcutType.SpellbookItem, spell4BaseId, tier);
                }
            }

            foreach (ClientRequestActionSetChanges.ActionTier actionTier in requestActionSetChanges.ActionTiers)
                session.Player.SpellManager.UpdateSpell(actionTier.Action, actionTier.Tier, requestActionSetChanges.ActionSetIndex);

            List<ushort> newAmps = GetDistinctNewAmpIds(actionSet, requestActionSetChanges.Amps)
                .ToList();

            if (newAmps.Count > 0)
            {
                foreach (ushort id in newAmps)
                    actionSet.AddAmp(id);
            }

            session.EnqueueMessageEncrypted(new ServerActionSetClearCache());
            session.EnqueueMessageEncrypted(actionSet.BuildServerActionSet());
            if (actionSet.TierPoints != previousTierPoints)
                session.Player.SpellManager.SendServerAbilityPoints();

            if (newAmps.Count > 0)
            {
                session.EnqueueMessageEncrypted(actionSet.BuildServerAmpList());
            }
        }

        private LimitedActionSetResult ValidateRequest(IWorldSession session, ClientRequestActionSetChanges requestActionSetChanges)
        {
            if (session.Player == null)
                return LimitedActionSetResult.InvalidUnit;

            if (requestActionSetChanges.ActionSetIndex >= ActionSet.MaxActionSets
                || requestActionSetChanges.ActionSetIndex != session.Player.SpellManager.ActiveActionSet)
            {
                return LimitedActionSetResult.InvalidSpecIndex;
            }

            if (!session.Player.IsAlive)
                return LimitedActionSetResult.PlayerIsDead;

            if (session.Player.InCombat)
                return LimitedActionSetResult.InCombat;

            if (requestActionSetChanges.Actions.Count != ClientRequestActionSetChanges.ActionCount)
                return LimitedActionSetResult.InvalidActionSetSize;

            IActionSet actionSet = session.Player.SpellManager.GetActionSet(requestActionSetChanges.ActionSetIndex);
            var requestedSpellTiers = new Dictionary<uint, byte>();
            var requestedSpellIds = new HashSet<uint>();

            for (int i = 0; i < requestActionSetChanges.Actions.Count; i++)
            {
                uint spell4BaseId = requestActionSetChanges.Actions[i];
                if (spell4BaseId == 0u)
                    continue;

                if (!requestedSpellIds.Add(spell4BaseId))
                    return LimitedActionSetResult.DuplicateSpell;

                ISpellBaseInfo spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(spell4BaseId);
                if (spellBaseInfo == null)
                    return LimitedActionSetResult.UnknownSpellId;

                if (session.Player.SpellManager.GetSpell(spell4BaseId) == null)
                    return LimitedActionSetResult.BadSpellInActionSet;

                bool isLimitedActionSlot = i < LimitedActionSlotCount;
                bool isLimitedActionSpell = spellBaseInfo.Entry.WeaponSlot == LimitedActionSpellWeaponSlot;
                if (isLimitedActionSlot != isLimitedActionSpell)
                    return LimitedActionSetResult.InvalidSlot;

                byte tier = actionSet.GetShortcut(ShortcutType.SpellbookItem, spell4BaseId)?.Tier ?? 1;
                requestedSpellTiers[spell4BaseId] = tier;
            }

            foreach (ClientRequestActionSetChanges.ActionTier actionTier in requestActionSetChanges.ActionTiers)
            {
                if (actionTier.Tier > ActionSet.MaxTier)
                    return LimitedActionSetResult.InvalidSpellTier;

                if (!requestedSpellTiers.ContainsKey(actionTier.Action))
                    return LimitedActionSetResult.LASChangeSpellFailed;

                ISpellBaseInfo spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(actionTier.Action);
                if (spellBaseInfo == null)
                    return LimitedActionSetResult.UnknownSpellId;

                if (spellBaseInfo.GetSpellInfo(actionTier.Tier) == null)
                    return LimitedActionSetResult.InvalidSpellTier;

                if (session.Player.SpellManager.GetSpell(actionTier.Action) == null)
                    return LimitedActionSetResult.LASChangeSpellFailed;

                requestedSpellTiers[actionTier.Action] = actionTier.Tier;
            }

            int requiredTierPoints = requestedSpellTiers.Values.Sum(CalculateTierCost);
            if (requiredTierPoints > ActionSet.MaxTierPoints)
                return LimitedActionSetResult.InsufficientAbilityPoints;

            List<ushort> newAmpIds = GetDistinctNewAmpIds(actionSet, requestActionSetChanges.Amps)
                .ToList();
            HashSet<ushort> selectedAmpIds = actionSet.Amps
                .Select(amp => checked((ushort)amp.Entry.Id))
                .Concat(newAmpIds)
                .ToHashSet();

            ushort requiredAmpPower = 0;
            foreach (ushort ampId in newAmpIds)
            {
                var entry = GameTableManager.Instance.EldanAugmentation.GetEntry(ampId);
                if (entry == null)
                    return LimitedActionSetResult.EldanAugmentationInvalidId;

                if (!IsValidForPlayerClass(session, entry))
                    return LimitedActionSetResult.UnknownClassId;

                if (!HasRequiredAmp(entry, selectedAmpIds))
                    return LimitedActionSetResult.EldanAugmentationInvalidSeries;

                requiredAmpPower += (ushort)entry.PowerCost;
                if (requiredAmpPower > actionSet.AmpPoints)
                    return LimitedActionSetResult.EldanAugmentationNotEnoughPower;
            }

            if (IsRestrictedInPvp(session.Player.Identity))
                return LimitedActionSetResult.RestrictedInPVP;

            return LimitedActionSetResult.Ok;
        }

        private bool IsRestrictedInPvp(Identity identity)
        {
            IMatch match = matchManager.GetMatchCharacter(identity).Match;
            if (match is not IPvpMatch pvpMatch)
                return false;

            uint flags = pvpMatch.MatchingMap?.GameTypeEntry?.MatchingGameTypeEnumFlags ?? 0u;

            return pvpMatch.State switch
            {
                PvpGameState.Preparation => (flags & PvpPreparationRestrictedFlag) != 0,
                PvpGameState.InProgress => (flags & PvpInProgressRestrictedFlag) != 0,
                _ => false
            };
        }

        private static IEnumerable<ushort> GetDistinctNewAmpIds(IActionSet actionSet, IEnumerable<ushort> requestedAmpIds)
        {
            HashSet<ushort> existingAmpIds = actionSet.Amps
                .Select(amp => (ushort)amp.Entry.Id)
                .ToHashSet();

            return requestedAmpIds
                .Distinct()
                .Where(ampId => !existingAmpIds.Contains(ampId));
        }

        private static bool IsValidForPlayerClass(IWorldSession session, EldanAugmentationEntry entry)
        {
            return entry.ClassId == 0u || entry.ClassId == (uint)session.Player.Class;
        }

        private static bool HasRequiredAmp(EldanAugmentationEntry entry, HashSet<ushort> selectedAmpIds)
        {
            if (entry.EldanAugmentationIdRequired == 0u)
                return true;

            return selectedAmpIds.Contains(checked((ushort)entry.EldanAugmentationIdRequired));
        }

        private static int CalculateTierCost(byte tier)
        {
            int cost = 0;
            for (byte currentTier = 2; currentTier <= tier; currentTier++)
                cost += currentTier == 5 || currentTier == 9 ? 5 : 1;

            return cost;
        }

        private static void SendActionSetResult(IWorldSession session, byte actionSetIndex, LimitedActionSetResult result)
        {
            session.EnqueueMessageEncrypted(new ServerActionSet
            {
                SpecIndex = actionSetIndex,
                Unlocked  = result == LimitedActionSetResult.InvalidSpecIndex ? (byte)0 : (byte)1,
                Result    = result
            });
        }
    }
}
