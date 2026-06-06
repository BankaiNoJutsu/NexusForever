using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Message;
using Microsoft.Extensions.Logging;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Costume
{
    public class ClientCostumeItemUnlockHandler : IMessageHandler<IWorldSession, ClientCostumeItemUnlock>
    {
        private readonly ILogger<ClientCostumeItemUnlockHandler> log;

        public ClientCostumeItemUnlockHandler(ILogger<ClientCostumeItemUnlockHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCostumeItemUnlock costumeItemUnlock)
        {
            IItem item = session.Player.Inventory.GetItem(costumeItemUnlock.Location);
            if (item == null)
            {
                log.LogWarning("Unhandled costume item unlock from player {PlayerGuid}: location={Location}, bagIndex={BagIndex}, result={Result}, reason=missing-item.",
                    session.Player?.Guid,
                    costumeItemUnlock.Location.Location,
                    costumeItemUnlock.Location.BagIndex,
                    CostumeUnlockResult.InvalidItem);
            }

            session.Player.Account.CostumeManager.UnlockItem(session.Player, item);
        }
    }
}
