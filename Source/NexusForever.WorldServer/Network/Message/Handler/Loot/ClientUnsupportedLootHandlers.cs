using Microsoft.Extensions.Logging;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Loot;
using NexusForever.WorldServer.Network.Message.Handler.Entity;

namespace NexusForever.WorldServer.Network.Message.Handler.Loot
{
    public class ClientLootItemHandler : IMessageHandler<IWorldSession, ClientLootItem>
    {
        private readonly ILogger<ClientLootItemHandler> log;
        private readonly IGlobalLootManager lootManager;

        public ClientLootItemHandler(
            ILogger<ClientLootItemHandler> log,
            IGlobalLootManager lootManager)
        {
            this.log = log;
            this.lootManager = lootManager;
        }

        public void HandleMessage(IWorldSession session, ClientLootItem lootItem)
        {
            if (session.Player == null)
                throw new InvalidPacketValueException();

            IWorldEntity owner = lootItem.OwnerUnitId == session.Player.Guid
                ? session.Player
                : session.Player.GetVisible<IWorldEntity>(lootItem.OwnerUnitId);
            if (owner == null)
                throw new InvalidPacketValueException();

            if (owner != session.Player && ActivationInteractionGuards.TryRejectBusyTarget(session, owner))
                return;

            if (lootItem.Request)
            {
                log.LogTrace("Loot notify request from player {PlayerGuid}: owner {OwnerUnitId}, loot {LootUnitId}.",
                    session.Player?.Guid, lootItem.OwnerUnitId, lootItem.LootUnitId);
                lootManager.SendLootNotify(session.Player, lootItem.OwnerUnitId);
                return;
            }

            log.LogTrace("Loot collect request from player {PlayerGuid}: owner {OwnerUnitId}, loot {LootUnitId}.",
                session.Player?.Guid, lootItem.OwnerUnitId, lootItem.LootUnitId);
            lootManager.GiveLoot(session.Player, lootItem.OwnerUnitId, lootItem.LootUnitId);
        }
    }

    public class ClientLootVacuumHandler : IMessageHandler<IWorldSession, ClientLootVacuum>
    {
        private readonly ILogger<ClientLootVacuumHandler> log;
        private readonly IGlobalLootManager lootManager;

        public ClientLootVacuumHandler(
            ILogger<ClientLootVacuumHandler> log,
            IGlobalLootManager lootManager)
        {
            this.log = log;
            this.lootManager = lootManager;
        }

        public void HandleMessage(IWorldSession session, ClientLootVacuum lootVacuum)
        {
            if (session.Player == null)
                throw new InvalidPacketValueException();

            log.LogTrace("Loot vacuum request from player {PlayerGuid}.", session.Player?.Guid);
            lootManager.GiveAllLootInRange(session.Player);
        }
    }

    public class ClientLootRollActionHandler : IMessageHandler<IWorldSession, ClientLootRollAction>
    {
        private readonly ILogger<ClientLootRollActionHandler> log;
        private readonly IGlobalLootManager lootManager;

        public ClientLootRollActionHandler(
            ILogger<ClientLootRollActionHandler> log,
            IGlobalLootManager lootManager)
        {
            this.log = log;
            this.lootManager = lootManager;
        }

        public void HandleMessage(IWorldSession session, ClientLootRollAction lootRollAction)
        {
            if (session.Player == null)
                throw new InvalidPacketValueException();

            log.LogTrace("Loot roll request from player {PlayerGuid}: owner {OwnerUnitId}, loot {LootUnitId}, action {Action}.",
                session.Player?.Guid, lootRollAction.OwnerUnitId, lootRollAction.LootUnitId, lootRollAction.Action);
            lootManager.RollLoot(session.Player, lootRollAction.OwnerUnitId, lootRollAction.LootUnitId, lootRollAction.Action);
        }
    }

    public class ClientLootAssignMasterHandler : IMessageHandler<IWorldSession, ClientLootAssignMaster>
    {
        private readonly ILogger<ClientLootAssignMasterHandler> log;
        private readonly IGlobalLootManager lootManager;

        public ClientLootAssignMasterHandler(
            ILogger<ClientLootAssignMasterHandler> log,
            IGlobalLootManager lootManager)
        {
            this.log = log;
            this.lootManager = lootManager;
        }

        public void HandleMessage(IWorldSession session, ClientLootAssignMaster lootAssignMaster)
        {
            if (session.Player == null)
                throw new InvalidPacketValueException();

            log.LogTrace("Master-loot assignment from player {PlayerGuid}: owner {OwnerUnitId}, loot {LootUnitId}, assignee {AssigneeId}.",
                session.Player?.Guid, lootAssignMaster.OwnerUnitId, lootAssignMaster.LootUnitId, lootAssignMaster.Assignee.Id);
            lootManager.AssignMasterLoot(session.Player, lootAssignMaster.OwnerUnitId, lootAssignMaster.LootUnitId, lootAssignMaster.Assignee.ToGameIdentity());
        }
    }
}
