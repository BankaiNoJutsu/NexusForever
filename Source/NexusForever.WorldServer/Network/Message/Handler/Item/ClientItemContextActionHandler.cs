using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.GameTable;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Item
{
    public class ClientItemContextActionHandler : IMessageHandler<IWorldSession, ClientItemContextAction>
    {
        #region Dependency Injection

        private readonly ILogger<ClientItemContextActionHandler> log;
        private readonly IGameTableManager gameTableManager;
        private readonly IPrerequisiteManager prerequisiteManager;

        public ClientItemContextActionHandler(
            ILogger<ClientItemContextActionHandler> log,
            IGameTableManager gameTableManager,
            IPrerequisiteManager prerequisiteManager = null)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
            this.prerequisiteManager = prerequisiteManager;
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

            bool handled = ItemUseHelper.TryUseItem(
                session,
                item,
                gameTableManager,
                clientRequestSource: nameof(ClientItemContextAction),
                selectedBranch: itemContextAction.SelectedBranch,
                prerequisiteManager: prerequisiteManager);

            if (!handled)
                log.LogTrace("ClientItemContextAction: player={Player} itemGuid={ItemGuid} branch={Branch} - no item-use handler matched.",
                    player.Guid, itemContextAction.ItemGuid, itemContextAction.SelectedBranch);
        }
    }
}
