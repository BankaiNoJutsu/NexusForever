using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientCommitAmpSpecHandler : IMessageHandler<IWorldSession, ClientCommitAmpSpec>
    {
        private readonly IGameTableManager gameTableManager;
        private readonly ILogger<ClientCommitAmpSpecHandler> log;

        public ClientCommitAmpSpecHandler(
            IGameTableManager gameTableManager,
            ILogger<ClientCommitAmpSpecHandler> log)
        {
            this.gameTableManager = gameTableManager;
            this.log              = log;
        }

        public void HandleMessage(IWorldSession session, ClientCommitAmpSpec commitAmpSpec)
        {
            LimitedActionSetResult validationResult = ValidateRequest(session, commitAmpSpec, out IActionSet actionSet, out List<EldanAugmentationEntry> newAmpEntries);
            if (validationResult != LimitedActionSetResult.Ok)
            {
                SendActionSetResult(session, session.Player?.SpellManager.ActiveActionSet ?? 0, validationResult);
                return;
            }

            foreach (EldanAugmentationEntry entry in newAmpEntries)
                actionSet.AddAmp(entry);

            log.LogDebug("Committed {AmpCount} AMP(s) for player {PlayerGuid} on spec {SpecIndex}.",
                newAmpEntries.Count, session.Player.Guid, actionSet.Index);

            session.EnqueueMessageEncrypted(actionSet.BuildServerAmpList());
        }

        private LimitedActionSetResult ValidateRequest(IWorldSession session, ClientCommitAmpSpec commitAmpSpec, out IActionSet actionSet, out List<EldanAugmentationEntry> newAmpEntries)
        {
            actionSet     = null;
            newAmpEntries = [];

            if (session.Player == null)
                return LimitedActionSetResult.InvalidUnit;

            byte actionSetIndex = session.Player.SpellManager.ActiveActionSet;
            if (actionSetIndex >= ActionSet.MaxActionSets)
                return LimitedActionSetResult.InvalidSpecIndex;

            if (!session.Player.IsAlive)
                return LimitedActionSetResult.PlayerIsDead;

            if (session.Player.InCombat)
                return LimitedActionSetResult.InCombat;

            actionSet = session.Player.SpellManager.GetActionSet(actionSetIndex);
            List<ushort> newAmpIds = GetDistinctNewAmpIds(actionSet, commitAmpSpec.Amps)
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

            return LimitedActionSetResult.Ok;
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
