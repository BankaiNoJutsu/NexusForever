using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.WorldServer.Network.Message.Handler.Entity;

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
            if (lootItem.Request)
            {
                log.LogDebug("Ignoring unsupported loot request from player {PlayerGuid}: owner {OwnerUnitId}, loot {LootUnitId}.",
                    session.Player?.Guid, lootItem.OwnerUnitId, lootItem.LootUnitId);
                return;
            }

            IWorldEntity lootEntity = session.Player.GetVisible<IWorldEntity>(lootItem.LootUnitId);
            if (lootEntity == null)
                throw new InvalidPacketValueException();

            if (ActivationInteractionGuards.TryRejectBusyTarget(session, lootEntity))
                return;

            if (ActivationInteractionGuards.TryRejectOutOfRangeTarget(session, lootEntity))
                return;

            log.LogDebug("Validated but unsupported loot collect request from player {PlayerGuid}: owner {OwnerUnitId}, loot {LootUnitId}.",
                session.Player?.Guid, lootItem.OwnerUnitId, lootItem.LootUnitId);
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
