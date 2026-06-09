using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientRequestActionSetChangesHandler : IMessageHandler<IWorldSession, ClientRequestActionSetChanges>
    {
        private const uint PvpPreparationRestrictedFlag = 0x40u;
        private const uint PvpInProgressRestrictedFlag = 0x04u;

        #region Dependency Injection

        private readonly IMatchManager matchManager;
        private readonly IGameTableManager gameTableManager;
        private readonly IGlobalSpellManager globalSpellManager;
        private readonly ILogger<ClientRequestActionSetChangesHandler> log;

        public ClientRequestActionSetChangesHandler(
            IMatchManager matchManager,
            IGameTableManager gameTableManager,
            IGlobalSpellManager globalSpellManager,
            ILogger<ClientRequestActionSetChangesHandler> log)
        {
            this.matchManager        = matchManager;
            this.gameTableManager   = gameTableManager;
            this.globalSpellManager = globalSpellManager;
            this.log                = log;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientRequestActionSetChanges requestActionSetChanges)
        {
            LimitedActionSetResult validationResult = ValidateRequest(session, requestActionSetChanges, out List<uint> requestedSpell4BaseIds, out List<EldanAugmentationEntry> newAmpEntries);
            if (validationResult != LimitedActionSetResult.Ok)
            {
                log.LogDebug(
                    "Rejecting action set changes for player {PlayerGuid}: spec {SpecIndex}, activeSpec {ActiveSpec}, result {Result}, actionCount {ActionCount}, tierCount {TierCount}, ampCount {AmpCount}, actions [{Actions}], resolvedActions [{ResolvedActions}].",
                    session.Player?.Guid,
                    requestActionSetChanges.ActionSetIndex,
                    session.Player?.SpellManager?.ActiveActionSet,
                    validationResult,
                    requestActionSetChanges.Actions.Count,
                    requestActionSetChanges.ActionTiers.Count,
                    requestActionSetChanges.Amps.Count,
                    string.Join(",", requestActionSetChanges.Actions),
                    string.Join(",", requestedSpell4BaseIds));
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

                uint spell4BaseId = requestedSpell4BaseIds[(int)i];
                if (spell4BaseId != 0u)
                {
                    IActionSetShortcut existingShortcut = spellShortcuts.SingleOrDefault(s => s.ObjectId == spell4BaseId);
                    byte tier = existingShortcut?.Tier ?? 1;
                    actionSet.AddShortcut(i, ShortcutType.SpellbookItem, spell4BaseId, tier);
                }
            }

            foreach (ClientRequestActionSetChanges.ActionTier actionTier in requestActionSetChanges.ActionTiers)
            {
                TryResolveSpell4BaseId(session, actionTier.Action, out uint spell4BaseId, out _);
                session.Player.SpellManager.UpdateSpell(spell4BaseId, actionTier.Tier, requestActionSetChanges.ActionSetIndex);
            }

            if (newAmpEntries.Count > 0)
            {
                foreach (EldanAugmentationEntry entry in newAmpEntries)
                    actionSet.AddAmp(entry);
            }

            session.Player.SpellManager.SendServerSpellList();
            session.EnqueueMessageEncrypted(new ServerActionSetClearCache());
            SendSelectedAbilityItems(session, requestedSpell4BaseIds);
            session.EnqueueMessageEncrypted(actionSet.BuildServerActionSet());
            if (actionSet.TierPoints != previousTierPoints)
                session.Player.SpellManager.SendServerAbilityPoints();

            if (newAmpEntries.Count > 0)
            {
                session.EnqueueMessageEncrypted(actionSet.BuildServerAmpList());
            }
        }

        private static void SendSelectedAbilityItems(IWorldSession session, IEnumerable<uint> spell4BaseIds)
        {
            foreach (uint spell4BaseId in spell4BaseIds.Where(id => id != 0u).Distinct())
            {
                ICharacterSpell spell = session.Player.SpellManager.GetSpell(spell4BaseId);
                if (spell?.Item == null)
                    continue;

                session.EnqueueMessageEncrypted(new ServerItemAdd
                {
                    InventoryItem = new NexusForever.Network.World.Message.Model.Shared.InventoryItem
                    {
                        Item   = spell.Item.Build(),
                        Reason = ItemUpdateReason.NoReason
                    }
                });
            }
        }

        private LimitedActionSetResult ValidateRequest(
            IWorldSession session,
            ClientRequestActionSetChanges requestActionSetChanges,
            out List<uint> requestedSpell4BaseIds,
            out List<EldanAugmentationEntry> newAmpEntries)
        {
            requestedSpell4BaseIds = [];
            newAmpEntries          = [];

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
                uint requestedSpellId = requestActionSetChanges.Actions[i];
                if (requestedSpellId == 0u)
                {
                    requestedSpell4BaseIds.Add(0u);
                    continue;
                }

                if (!TryResolveSpell4BaseId(session, requestedSpellId, out uint spell4BaseId, out ISpellBaseInfo spellBaseInfo))
                    return LimitedActionSetResult.UnknownSpellId;

                requestedSpell4BaseIds.Add(spell4BaseId);

                if (!requestedSpellIds.Add(spell4BaseId))
                    return LimitedActionSetResult.DuplicateSpell;

                if (session.Player.SpellManager.GetSpell(spell4BaseId) == null)
                    return LimitedActionSetResult.BadSpellInActionSet;

                byte tier = actionSet.GetShortcut(ShortcutType.SpellbookItem, spell4BaseId)?.Tier ?? 1;
                requestedSpellTiers[spell4BaseId] = tier;
            }

            foreach (ClientRequestActionSetChanges.ActionTier actionTier in requestActionSetChanges.ActionTiers)
            {
                if (actionTier.Tier > ActionSet.MaxTier)
                    return LimitedActionSetResult.InvalidSpellTier;

                if (!TryResolveSpell4BaseId(session, actionTier.Action, out uint spell4BaseId, out ISpellBaseInfo spellBaseInfo))
                    return LimitedActionSetResult.UnknownSpellId;

                if (!requestedSpellTiers.ContainsKey(spell4BaseId))
                    return LimitedActionSetResult.LASChangeSpellFailed;

                if (spellBaseInfo.GetSpellInfo(actionTier.Tier) == null)
                    return LimitedActionSetResult.InvalidSpellTier;

                if (session.Player.SpellManager.GetSpell(spell4BaseId) == null)
                    return LimitedActionSetResult.LASChangeSpellFailed;

                requestedSpellTiers[spell4BaseId] = actionTier.Tier;
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
                var entry = gameTableManager.EldanAugmentation?.GetEntry(ampId);
                if (entry == null)
                    return LimitedActionSetResult.EldanAugmentationInvalidId;

                if (!IsValidForPlayerClass(session, entry))
                    return LimitedActionSetResult.UnknownClassId;

                if (!HasRequiredAmp(entry, selectedAmpIds))
                    return LimitedActionSetResult.EldanAugmentationInvalidSeries;

                requiredAmpPower += (ushort)entry.PowerCost;
                if (requiredAmpPower > actionSet.AmpPoints)
                    return LimitedActionSetResult.EldanAugmentationNotEnoughPower;

                newAmpEntries.Add(entry);
            }

            if (IsRestrictedInPvp(session.Player.Identity))
                return LimitedActionSetResult.RestrictedInPVP;

            return LimitedActionSetResult.Ok;
        }

        private bool TryResolveSpell4BaseId(IWorldSession session, uint requestedSpellId, out uint spell4BaseId, out ISpellBaseInfo spellBaseInfo)
        {
            if (TryGetSpellBaseInfo(requestedSpellId, out ISpellBaseInfo directSpellBaseInfo)
                && session.Player.SpellManager.GetSpell(requestedSpellId) != null)
            {
                spell4BaseId = requestedSpellId;
                spellBaseInfo = directSpellBaseInfo;
                return true;
            }

            Spell4Entry spell4Entry = gameTableManager.Spell4?.GetEntry(requestedSpellId);
            if (spell4Entry != null
                && TryGetSpellBaseInfo(spell4Entry.Spell4BaseIdBaseSpell, out ISpellBaseInfo mappedSpellBaseInfo))
            {
                spell4BaseId = spell4Entry.Spell4BaseIdBaseSpell;
                spellBaseInfo = mappedSpellBaseInfo;
                return true;
            }

            if (directSpellBaseInfo != null)
            {
                spell4BaseId = requestedSpellId;
                spellBaseInfo = directSpellBaseInfo;
                return true;
            }

            spell4BaseId = 0u;
            spellBaseInfo = null;
            return false;
        }

        private bool TryGetSpellBaseInfo(uint spell4BaseId, out ISpellBaseInfo spellBaseInfo)
        {
            try
            {
                spellBaseInfo = globalSpellManager.GetSpellBaseInfo(spell4BaseId);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                spellBaseInfo = null;
            }

            return spellBaseInfo != null;
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
