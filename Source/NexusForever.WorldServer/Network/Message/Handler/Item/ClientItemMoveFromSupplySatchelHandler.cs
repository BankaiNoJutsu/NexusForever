using NexusForever.Network.Message;
using NexusForever.Game.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Item
{
    public class ClientItemMoveFromSupplySatchelHandler : IMessageHandler<IWorldSession, ClientItemMoveFromSupplySatchel>
    {
        public void HandleMessage(IWorldSession session, ClientItemMoveFromSupplySatchel request)
        {
            var satchel = session.Player.SupplySatchelManager;
            if (session.Player is Player && session is GameSession connection)
            {
                if (satchel is SupplySatchelManager manager)
                    manager.MoveToInventoryFromClient(connection, request.MaterialId, request.Amount);
                return;
            }

            satchel.MoveToInventory(request.MaterialId, request.Amount);
        }
    }
}
