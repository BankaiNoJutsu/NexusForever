using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Account
{
    public class ClientAccountItemTakeHandler : IMessageHandler<IWorldSession, ClientAccountItemTake>
    {
        private readonly ILogger<ClientAccountItemTakeHandler> log;

        public ClientAccountItemTakeHandler(ILogger<ClientAccountItemTakeHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientAccountItemTake accountItemTake)
        {
            GenericError result = session.Account.InventoryManager.TakeItem(session.Player, accountItemTake.Id);
            if (result == GenericError.Ok)
                return;

            log.LogDebug("Rejecting account item take from player {PlayerGuid}: account inventory id {InventoryId}, error {Error}.",
                session.Player?.Guid, accountItemTake.Id, result);
            session.Player?.SendGenericError(result);
        }
    }

    public class ClientAccountItemClaimPendingItemGroupHandler : IMessageHandler<IWorldSession, ClientAccountItemClaimPendingItemGroup>
    {
        private readonly ILogger<ClientAccountItemClaimPendingItemGroupHandler> log;

        public ClientAccountItemClaimPendingItemGroupHandler(ILogger<ClientAccountItemClaimPendingItemGroupHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientAccountItemClaimPendingItemGroup claimPendingItemGroup)
        {
            log.LogDebug("Rejecting unsupported pending account item group claim from player {PlayerGuid}: group {Group}.",
                session.Player?.Guid, claimPendingItemGroup.Group);
            session.Account.InventoryManager.SendPendingItems();
            session.Player?.SendGenericError(GenericError.Params);
        }
    }

    public class ClientAccountItemReturnPendingItemGroupHandler : IMessageHandler<IWorldSession, ClientAccountItemReturnPendingItemGroup>
    {
        private readonly ILogger<ClientAccountItemReturnPendingItemGroupHandler> log;

        public ClientAccountItemReturnPendingItemGroupHandler(ILogger<ClientAccountItemReturnPendingItemGroupHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientAccountItemReturnPendingItemGroup returnPendingItemGroup)
        {
            log.LogDebug("Rejecting unsupported pending account item group return from player {PlayerGuid}: group {Group}.",
                session.Player?.Guid, returnPendingItemGroup.Group);
            session.Account.InventoryManager.SendPendingItems();
            session.Player?.SendGenericError(GenericError.Params);
        }
    }
}
