using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    public class ClientResetInstancesHandler : IMessageHandler<IWorldSession, ClientResetInstances>
    {
        private readonly ILogger<ClientResetInstancesHandler> log;
        private readonly IMapLockManager mapLockManager;

        public ClientResetInstancesHandler(ILogger<ClientResetInstancesHandler> log, IMapLockManager mapLockManager)
        {
            this.log = log;
            this.mapLockManager = mapLockManager;
        }

        public void HandleMessage(IWorldSession session, ClientResetInstances _)
        {
            IPlayer player = session.Player;
            if (player == null)
                return;
            if (player.IsLoading || !player.CanTeleport())
            {
                player.SendSystemMessage("Instances cannot be reset while travelling.");
                return;
            }

            IMapLock current = (player.Map as IMapInstance)?.MapLock;
            int reset = 0;
            foreach (IMapLock mapLock in mapLockManager.GetSoloLocks(player.Identity))
            {
                if (mapLock.InstanceId == current?.InstanceId)
                    continue;
                if (mapLockManager.RemoveSoloLock(player.Identity, mapLock.WorldId, mapLock.InstanceId))
                    reset++;
            }

            log.LogDebug("Reset {Count} solo instances for player {PlayerGuid}.", reset, player.Guid);
            // /resetinstances does not open the Instance Settings dialog. Its result event
            // is only safe for that dialog's reset button; use chat for this command.
            player.SendSystemMessage(reset == 0 ? "You have no instances to reset."
                : current != null ? "Your instances have been reset. The instance you are in was kept."
                : "Your instances have been reset.");
        }
    }
}
