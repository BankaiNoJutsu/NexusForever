using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientRequestActionSetChangesHandler : IMessageHandler<IWorldSession, ClientRequestActionSetChanges>
    {
        private const int LimitedActionSlotCount = 8;
        private const uint LimitedActionSpellWeaponSlot = 5u;

        public void HandleMessage(IWorldSession session, ClientRequestActionSetChanges requestActionSetChanges)
        {
            // TODO: check for client validity, e.g. Level & Spell4TierRequirements

            if (requestActionSetChanges.ActionSetIndex >= ActionSet.MaxActionSets)
            {
                SendActionSetResult(session, requestActionSetChanges.ActionSetIndex, LimitedActionSetResult.InvalidSpecIndex);
                return;
            }

            LimitedActionSetResult validationResult = ValidateRequest(session, requestActionSetChanges);
            if (validationResult != LimitedActionSetResult.Ok)
            {
                SendActionSetResult(session, requestActionSetChanges.ActionSetIndex, validationResult);
                return;
            }

            IActionSet actionSet = session.Player.SpellManager.GetActionSet(requestActionSetChanges.ActionSetIndex);

            List<IActionSetShortcut> shortcuts = actionSet.Actions.ToList();
            for (UILocation i = 0; i < (UILocation)requestActionSetChanges.Actions.Count; i++)
            {
                IActionSetShortcut shortcut = actionSet.GetShortcut(i);
                if (shortcut != null)
                    actionSet.RemoveShortcut(i);

                uint spell4BaseId = requestActionSetChanges.Actions[(int)i];
                if (spell4BaseId != 0u)
                {
                    IActionSetShortcut existingShortcut = shortcuts.SingleOrDefault(s => s.ObjectId == spell4BaseId);
                    byte tier = existingShortcut?.Tier ?? 1;
                    actionSet.AddShortcut(i, ShortcutType.SpellbookItem, spell4BaseId, tier);
                }
            }

            foreach (ClientRequestActionSetChanges.ActionTier actionTier in requestActionSetChanges.ActionTiers)
                session.Player.SpellManager.UpdateSpell(actionTier.Action, actionTier.Tier, requestActionSetChanges.ActionSetIndex);

            session.EnqueueMessageEncrypted(actionSet.BuildServerActionSet());
            if (requestActionSetChanges.ActionTiers.Count > 0)
                session.Player.SpellManager.SendServerAbilityPoints();

            // only new AMP can be added with this packet, filter out existing ones
            List<ushort> newAmps = requestActionSetChanges.Amps
                .Except(actionSet.Amps
                    .Select(a => (ushort)a.Entry.Id))
                .ToList();

            if (newAmps.Count > 0)
            {
                foreach (ushort id in newAmps)
                    actionSet.AddAmp(id);

                session.EnqueueMessageEncrypted(actionSet.BuildServerAmpList());
            }
        }

        private static LimitedActionSetResult ValidateRequest(IWorldSession session, ClientRequestActionSetChanges requestActionSetChanges)
        {
            if (requestActionSetChanges.Actions.Count != ClientRequestActionSetChanges.ActionCount)
                return LimitedActionSetResult.InvalidActionSetSize;

            for (int i = 0; i < requestActionSetChanges.Actions.Count; i++)
            {
                uint spell4BaseId = requestActionSetChanges.Actions[i];
                if (spell4BaseId == 0u)
                    continue;

                ISpellBaseInfo spellBaseInfo = GlobalSpellManager.Instance.GetSpellBaseInfo(spell4BaseId);
                if (spellBaseInfo == null)
                    return LimitedActionSetResult.UnknownSpellId;

                if (session.Player.SpellManager.GetSpell(spell4BaseId) == null)
                    return LimitedActionSetResult.BadSpellInActionSet;

                bool isLimitedActionSlot = i < LimitedActionSlotCount;
                bool isLimitedActionSpell = spellBaseInfo.Entry.WeaponSlot == LimitedActionSpellWeaponSlot;
                if (isLimitedActionSlot != isLimitedActionSpell)
                    return LimitedActionSetResult.InvalidSlot;
            }

            foreach (ClientRequestActionSetChanges.ActionTier actionTier in requestActionSetChanges.ActionTiers)
            {
                if (actionTier.Tier > ActionSet.MaxTier)
                    return LimitedActionSetResult.InvalidSpellTier;

                if (session.Player.SpellManager.GetSpell(actionTier.Action) == null)
                    return LimitedActionSetResult.LASChangeSpellFailed;
            }

            return LimitedActionSetResult.Ok;
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
