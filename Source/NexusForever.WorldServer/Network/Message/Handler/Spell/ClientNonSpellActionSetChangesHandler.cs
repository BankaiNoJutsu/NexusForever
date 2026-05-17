using System;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Abilities;
using NexusForever.Game.Static.Spell;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientNonSpellActionSetChangesHandler : IMessageHandler<IWorldSession, ClientNonSpellActionSetChanges>
    {
        private readonly IGameTableManager gameTableManager;

        public ClientNonSpellActionSetChangesHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientNonSpellActionSetChanges requestActionSetChanges)
        {
            ValidateRequest(requestActionSetChanges);

            IActionSet actionSet = session.Player.SpellManager.GetActionSet(requestActionSetChanges.SpecIndex);
            IActionSetShortcut existingShortcut = actionSet.GetShortcut(requestActionSetChanges.ActionBarIndex);

            if (requestActionSetChanges.ShortcutType == ShortcutType.None)
            {
                if (existingShortcut != null)
                    actionSet.RemoveShortcut(requestActionSetChanges.ActionBarIndex);

                return;
            }

            if (existingShortcut != null)
                actionSet.RemoveShortcut(requestActionSetChanges.ActionBarIndex);

            actionSet.AddShortcut(requestActionSetChanges.ActionBarIndex, requestActionSetChanges.ShortcutType, requestActionSetChanges.ObjectId, 0);
        }

        private void ValidateRequest(ClientNonSpellActionSetChanges requestActionSetChanges)
        {
            if (requestActionSetChanges.SpecIndex >= ActionSet.MaxActionSets)
                throw new InvalidPacketValueException($"Invalid action set index received: {requestActionSetChanges.SpecIndex}");

            if (requestActionSetChanges.ActionBarIndex == UILocation.None
                || (uint)requestActionSetChanges.ActionBarIndex >= ActionSet.MaxActionCount)
            {
                throw new InvalidPacketValueException($"Invalid action bar index received: {requestActionSetChanges.ActionBarIndex}");
            }

            switch (requestActionSetChanges.ShortcutType)
            {
                case ShortcutType.None:
                    if (requestActionSetChanges.ObjectId != 0u)
                        throw new InvalidPacketValueException($"Invalid object id for shortcut removal: {requestActionSetChanges.ObjectId}");
                    break;
                case ShortcutType.BagItem:
                    if (requestActionSetChanges.ObjectId == 0u
                        || gameTableManager.Item.GetEntry(requestActionSetChanges.ObjectId) == null)
                    {
                        throw new InvalidPacketValueException($"Invalid bag item shortcut received: {requestActionSetChanges.ObjectId}");
                    }
                    break;
                case ShortcutType.Macro:
                    if (requestActionSetChanges.ObjectId == 0u)
                        throw new InvalidPacketValueException("Invalid macro shortcut id received: 0");
                    break;
                case ShortcutType.GameCommand:
                    if (!Enum.IsDefined(typeof(GameCommandType), (GameCommandType)requestActionSetChanges.ObjectId))
                        throw new InvalidPacketValueException($"Invalid game command shortcut received: {requestActionSetChanges.ObjectId}");
                    break;
                case ShortcutType.SpellbookItem:
                default:
                    throw new InvalidPacketValueException($"Invalid non-spell shortcut type received: {requestActionSetChanges.ShortcutType}");
            }
        }
    }
}
