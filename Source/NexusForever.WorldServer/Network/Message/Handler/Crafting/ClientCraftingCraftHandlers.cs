using Microsoft.Extensions.Logging;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;

namespace NexusForever.WorldServer.Network.Message.Handler.Crafting
{
    public class ClientCraftingSimpleCraftHandler : IMessageHandler<IWorldSession, ClientCraftingSimpleCraft>
    {
        private readonly ILogger<ClientCraftingSimpleCraftHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientCraftingSimpleCraftHandler(
            ILogger<ClientCraftingSimpleCraftHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingSimpleCraft craft)
        {
            TradeskillSchematic2Entry schematic = CraftingCraftRequestHelper.GetSchematic(gameTableManager, craft.TradeskillSchematic2Id);

            log.LogDebug("Rejecting unsupported simple craft request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}.",
                session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id);

            CraftingCraftRequestHelper.SendCraftFailure(session, schematic);
        }
    }

    public class ClientCraftingComplexCraftHandler : IMessageHandler<IWorldSession, ClientCraftingComplexCraft>
    {
        private readonly ILogger<ClientCraftingComplexCraftHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientCraftingComplexCraftHandler(
            ILogger<ClientCraftingComplexCraftHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingComplexCraft craft)
        {
            TradeskillSchematic2Entry schematic = CraftingCraftRequestHelper.GetSchematic(gameTableManager, craft.TradeskillSchematic2Id);
            CraftingCraftRequestHelper.ValidateItem(gameTableManager, craft.PowerCoreItem2Id);

            log.LogDebug("Rejecting unsupported complex craft request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, powerCore {PowerCoreItem2Id}, charges {ChargeCount}.",
                session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, craft.PowerCoreItem2Id, craft.ChargeCounts?.Length ?? 0);

            CraftingCraftRequestHelper.SendCraftFailure(session, schematic);
        }
    }

    public class ClientCraftingCraftItemHandler : IMessageHandler<IWorldSession, ClientCraftingCraftItem>
    {
        private readonly ILogger<ClientCraftingCraftItemHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientCraftingCraftItemHandler(
            ILogger<ClientCraftingCraftItemHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingCraftItem craft)
        {
            TradeskillSchematic2Entry schematic = CraftingCraftRequestHelper.GetSchematic(gameTableManager, craft.TradeskillSchematic2Id);
            CraftingCraftRequestHelper.ValidateItem(gameTableManager, craft.CatalystItem2Id);

            log.LogDebug("Rejecting unsupported craft-item request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, count {SchematicCount}, catalyst {CatalystItem2Id}.",
                session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, craft.SchematicCount, craft.CatalystItem2Id);

            CraftingCraftRequestHelper.SendCraftFailure(session, schematic);
        }
    }

    public class ClientCraftingCraftItemAutoCraftHandler : IMessageHandler<IWorldSession, ClientCraftingCraftItemAutoCraft>
    {
        private readonly ILogger<ClientCraftingCraftItemAutoCraftHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientCraftingCraftItemAutoCraftHandler(
            ILogger<ClientCraftingCraftItemAutoCraftHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientCraftingCraftItemAutoCraft craft)
        {
            TradeskillSchematic2Entry schematic = CraftingCraftRequestHelper.GetSchematic(gameTableManager, craft.TradeskillSchematic2Id);

            log.LogDebug("Rejecting unsupported auto-craft request from player {PlayerGuid}: context {ContextToken}, station {StationUnitId}, schematic {SchematicId}, count {SchematicCount}.",
                session.Player?.Guid, craft.ContextToken, craft.CraftingStationUnitId, craft.TradeskillSchematic2Id, craft.SchematicCount);

            CraftingCraftRequestHelper.SendCraftFailure(session, schematic);
        }
    }

    internal static class CraftingCraftRequestHelper
    {
        public static TradeskillSchematic2Entry GetSchematic(IGameTableManager gameTableManager, uint tradeskillSchematic2Id)
        {
            TradeskillSchematic2Entry schematic = gameTableManager.TradeskillSchematic2.GetEntry(tradeskillSchematic2Id);
            if (schematic == null)
                throw new InvalidPacketValueException();

            return schematic;
        }

        public static void ValidateItem(IGameTableManager gameTableManager, uint item2Id)
        {
            if (item2Id == 0u)
                return;

            if (gameTableManager.Item.GetEntry(item2Id) == null)
                throw new InvalidPacketValueException();
        }

        public static void SendCraftFailure(IWorldSession session, TradeskillSchematic2Entry schematic)
        {
            session.EnqueueMessageEncrypted(new ServerCraftingFinish
            {
                Pass = false,
                TradeskillSchematic2IdCrafted = schematic.Id
            });
        }
    }
}
