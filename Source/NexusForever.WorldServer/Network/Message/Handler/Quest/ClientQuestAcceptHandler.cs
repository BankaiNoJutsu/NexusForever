using System;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network.Message.Handler;

namespace NexusForever.WorldServer.Network.Message.Handler.Quest
{
    public class ClientQuestAcceptHandler : IMessageHandler<IWorldSession, ClientQuestAccept>
    {
        public void HandleMessage(IWorldSession session, ClientQuestAccept questAccept)
        {
            var player = session.Player;
            if (player is Player actor && !ReferenceEquals(actor.Session, session)) return;

            IItem item = null;
            bool startedFromItem = questAccept.ItemLocation.Location != (InventoryLocation)300;
            if (startedFromItem)
            {
                item = player.Inventory.GetItem(questAccept.ItemLocation);
                if (item == null)
                    throw new InvalidPacketValueException();
            }

            Action closeDialog = startedFromItem ? null : () => DialogSessionState.EndActiveDialog(session);
            if (player is Player && player.QuestManager is QuestManager quests)
                quests.QuestAddFromClient(questAccept.QuestId, item, closeDialog);
            else
            {
                player.QuestManager.QuestAdd(questAccept.QuestId, item);
                closeDialog?.Invoke();
            }
        }
    }
}
