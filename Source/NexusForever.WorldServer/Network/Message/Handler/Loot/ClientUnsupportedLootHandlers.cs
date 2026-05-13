using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Loot;

namespace NexusForever.WorldServer.Network.Message.Handler.Loot
{
    public class ClientLootItemHandler : IMessageHandler<IWorldSession, ClientLootItem>
    {
        private readonly ILogger<ClientLootItemHandler> log;

        public ClientLootItemHandler(ILogger<ClientLootItemHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientLootItem lootItem)
        {
            log.LogDebug("Ignoring unsupported loot item request from player {PlayerGuid}: owner {OwnerUnitId}, loot {LootUnitId}, request {Request}.",
                session.Player?.Guid, lootItem.OwnerUnitId, lootItem.LootUnitId, lootItem.Request);
        }
    }

    public class ClientLootVacuumHandler : IMessageHandler<IWorldSession, ClientLootVacuum>
    {
        private readonly ILogger<ClientLootVacuumHandler> log;

        public ClientLootVacuumHandler(ILogger<ClientLootVacuumHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientLootVacuum lootVacuum)
        {
            log.LogDebug("Ignoring unsupported loot vacuum request from player {PlayerGuid}.", session.Player?.Guid);
        }
    }

    public class ClientLootRollActionHandler : IMessageHandler<IWorldSession, ClientLootRollAction>
    {
        private readonly ILogger<ClientLootRollActionHandler> log;

        public ClientLootRollActionHandler(ILogger<ClientLootRollActionHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientLootRollAction lootRollAction)
        {
            log.LogDebug("Ignoring unsupported loot roll request from player {PlayerGuid}: owner {OwnerUnitId}, loot {LootUnitId}, action {Action}.",
                session.Player?.Guid, lootRollAction.OwnerUnitId, lootRollAction.LootUnitId, lootRollAction.Action);
        }
    }

    public class ClientLootAssignMasterHandler : IMessageHandler<IWorldSession, ClientLootAssignMaster>
    {
        private readonly ILogger<ClientLootAssignMasterHandler> log;

        public ClientLootAssignMasterHandler(ILogger<ClientLootAssignMasterHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientLootAssignMaster lootAssignMaster)
        {
            log.LogDebug("Ignoring unsupported master-loot assignment from player {PlayerGuid}: owner {OwnerUnitId}, loot {LootUnitId}, assignee {AssigneeId}.",
                session.Player?.Guid, lootAssignMaster.OwnerUnitId, lootAssignMaster.LootUnitId, lootAssignMaster.Assignee.Id);
        }
    }
}
