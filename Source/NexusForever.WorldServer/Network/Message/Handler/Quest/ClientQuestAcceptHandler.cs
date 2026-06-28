using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network.Message.Handler;

namespace NexusForever.WorldServer.Network.Message.Handler.Quest
{
    public class ClientQuestAcceptHandler : IMessageHandler<IWorldSession, ClientQuestAccept>
    {
        public void HandleMessage(IWorldSession session, ClientQuestAccept questAccept)
        {
            IItem item = null;
            bool startedFromItem = questAccept.ItemLocation.Location != (InventoryLocation)300;
            if (startedFromItem)
                item = session.Player.Inventory.GetItem(questAccept.ItemLocation);

            session.Player.QuestManager.QuestAdd(questAccept.QuestId, item);

            if (!startedFromItem)
                DialogSessionState.EndActiveDialog(session);
        }
    }
}
