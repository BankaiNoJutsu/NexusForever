using NexusForever.Game.Abstract.Entity;
using NexusForever.GameTable;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Item
{
    public class ClientItemUseHandler : IMessageHandler<IWorldSession, ClientItemUse>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public ClientItemUseHandler(IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientItemUse itemUse)
        {
            IItem item = session.Player.Inventory.GetItem(itemUse.Location);
            if (item == null)
                throw new InvalidPacketValueException();

            ItemUseHelper.TryUseItem(
                session,
                item,
                gameTableManager,
                itemUse.TargetUnitId,
                itemUse.Position,
                itemUse.ContextToken,
                nameof(ClientItemUse),
                applyPendingSpellEvidenceCapture: true);
        }
    }
}
