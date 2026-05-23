using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Item
{
    public class ClientItemContextActionHandler : IMessageHandler<IWorldSession, ClientItemContextAction>
    {
        #region Dependency Injection

        private readonly ILogger<ClientItemContextActionHandler> log;

        public ClientItemContextActionHandler(ILogger<ClientItemContextActionHandler> log)
        {
            this.log = log;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientItemContextAction itemContextAction)
        {
            IPlayer player = session.Player;
            if (player?.Inventory == null)
                return;

            var item = player.Inventory.GetItem(itemContextAction.ItemGuid);
            if (item == null)
            {
                log.LogDebug("ClientItemContextAction: player={Player} itemGuid={ItemGuid} - item not found.",
                    player.Guid, itemContextAction.ItemGuid);
                return;
            }

            log.LogTrace("ClientItemContextAction: player={Player} itemGuid={ItemGuid} branch={Branch}",
                player.Guid, itemContextAction.ItemGuid, itemContextAction.SelectedBranch);

            // SelectedBranch is still diagnostic-only; actual item mutations are
            // handled by dedicated opcodes such as ClientItemUse (0x0943).
        }
    }
}
