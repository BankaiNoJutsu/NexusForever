using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Trade
{
    public class ClientP2PTradingAcceptInviteHandler : IMessageHandler<IWorldSession, ClientP2PTradingAcceptInvite>
    {
        private readonly ILogger<ClientP2PTradingAcceptInviteHandler> log;
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingAcceptInviteHandler(
            ILogger<ClientP2PTradingAcceptInviteHandler> log,
            ITradeManager tradeManager)
        {
            this.log          = log;
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingAcceptInvite acceptInvite)
        {
            ServerP2PTradeResult.P2PTradeResult? result = tradeManager.Accept(session.Player);
            TradeRequestHelper.SendTradeResult(session, result);

            log.LogDebug("Processed P2P trade accept-invite request from player {PlayerGuid}: result {Result}.",
                session.Player?.Guid, result?.ToString() ?? "Accepted");
        }
    }

    public class ClientP2PTradingDeclineInviteHandler : IMessageHandler<IWorldSession, ClientP2PTradingDeclineInvite>
    {
        private readonly ILogger<ClientP2PTradingDeclineInviteHandler> log;
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingDeclineInviteHandler(
            ILogger<ClientP2PTradingDeclineInviteHandler> log,
            ITradeManager tradeManager)
        {
            this.log          = log;
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingDeclineInvite declineInvite)
        {
            ServerP2PTradeResult.P2PTradeResult? result = tradeManager.Decline(session.Player);
            TradeRequestHelper.SendTradeResult(session, result);

            log.LogDebug("Processed P2P trade decline-invite request from player {PlayerGuid}: result {Result}.",
                session.Player?.Guid, result?.ToString() ?? "Declined");
        }
    }

    public class ClientP2PTradingCancelTradeHandler : IMessageHandler<IWorldSession, ClientP2PTradingCancelTrade>
    {
        private readonly ILogger<ClientP2PTradingCancelTradeHandler> log;
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingCancelTradeHandler(
            ILogger<ClientP2PTradingCancelTradeHandler> log,
            ITradeManager tradeManager)
        {
            this.log          = log;
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingCancelTrade cancelTrade)
        {
            ServerP2PTradeResult.P2PTradeResult? result = tradeManager.Cancel(session.Player);
            TradeRequestHelper.SendTradeResult(session, result);

            log.LogDebug("Processed P2P trade cancel request from player {PlayerGuid}: result {Result}.",
                session.Player?.Guid, result?.ToString() ?? "Canceled");
        }
    }

    public class ClientP2PTradingCommitHandler : IMessageHandler<IWorldSession, ClientP2PTradingCommit>
    {
        private readonly ILogger<ClientP2PTradingCommitHandler> log;
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingCommitHandler(
            ILogger<ClientP2PTradingCommitHandler> log,
            ITradeManager tradeManager)
        {
            this.log          = log;
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingCommit commit)
        {
            ServerP2PTradeResult.P2PTradeResult? result = tradeManager.Commit(session.Player);
            TradeRequestHelper.SendTradeResult(session, result);

            log.LogDebug("Processed P2P trade commit request from player {PlayerGuid}: result {Result}.",
                session.Player?.Guid, result?.ToString() ?? "Committed");
        }
    }

    public class ClientP2PTradingInitiateTradeHandler : IMessageHandler<IWorldSession, ClientP2PTradingInitiateTrade>
    {
        private readonly ILogger<ClientP2PTradingInitiateTradeHandler> log;
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingInitiateTradeHandler(
            ILogger<ClientP2PTradingInitiateTradeHandler> log,
            ITradeManager tradeManager)
        {
            this.log          = log;
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingInitiateTrade initiateTrade)
        {
            if (initiateTrade.TargetUnitId == 0u || initiateTrade.TargetUnitId == session.Player.Guid)
                throw new InvalidPacketValueException();

            IPlayer target = session.Player.GetVisible<IPlayer>(initiateTrade.TargetUnitId);
            ServerP2PTradeResult.P2PTradeResult? result = tradeManager.Initiate(session.Player, target);
            TradeRequestHelper.SendTradeResult(session, result);

            log.LogDebug("Processed P2P trade initiate request from player {PlayerGuid}: target {TargetUnitId}, result {Result}.",
                session.Player?.Guid, initiateTrade.TargetUnitId, result?.ToString() ?? "Invited");
        }
    }

    public class ClientP2PTradingAddItemHandler : IMessageHandler<IWorldSession, ClientP2PTradingAddItem>
    {
        private readonly ILogger<ClientP2PTradingAddItemHandler> log;
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingAddItemHandler(
            ILogger<ClientP2PTradingAddItemHandler> log,
            ITradeManager tradeManager)
        {
            this.log          = log;
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingAddItem addItem)
        {
            IItem item = TradeRequestHelper.GetInventoryItem(session, addItem.ItemGuid);
            ServerP2PTradeResult.P2PTradeResult? result = tradeManager.AddItem(session.Player, item);
            TradeRequestHelper.SendTradeResult(session, result);

            log.LogDebug("Processed P2P trade add-item request from player {PlayerGuid}: item {ItemGuid}, result {Result}.",
                session.Player?.Guid, addItem.ItemGuid, result?.ToString() ?? "Added");
        }
    }

    public class ClientP2PTradingRemoveItemHandler : IMessageHandler<IWorldSession, ClientP2PTradingRemoveItem>
    {
        private readonly ILogger<ClientP2PTradingRemoveItemHandler> log;
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingRemoveItemHandler(
            ILogger<ClientP2PTradingRemoveItemHandler> log,
            ITradeManager tradeManager)
        {
            this.log          = log;
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingRemoveItem removeItem)
        {
            TradeRequestHelper.GetInventoryItem(session, removeItem.ItemGuid);
            ServerP2PTradeResult.P2PTradeResult? result = tradeManager.RemoveItem(session.Player, removeItem.ItemGuid);
            TradeRequestHelper.SendTradeResult(session, result);

            log.LogDebug("Processed P2P trade remove-item request from player {PlayerGuid}: item {ItemGuid}, result {Result}.",
                session.Player?.Guid, removeItem.ItemGuid, result?.ToString() ?? "Removed");
        }
    }

    public class ClientP2PTradingSetMoneyHandler : IMessageHandler<IWorldSession, ClientP2PTradingSetMoney>
    {
        private readonly ILogger<ClientP2PTradingSetMoneyHandler> log;
        private readonly ITradeManager tradeManager;

        public ClientP2PTradingSetMoneyHandler(
            ILogger<ClientP2PTradingSetMoneyHandler> log,
            ITradeManager tradeManager)
        {
            this.log          = log;
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingSetMoney setMoney)
        {
            if (!session.Player.CurrencyManager.CanAfford(CurrencyType.Credits, setMoney.Credits))
                throw new InvalidPacketValueException();

            ServerP2PTradeResult.P2PTradeResult? result = tradeManager.SetMoney(session.Player, setMoney.Credits);
            TradeRequestHelper.SendTradeResult(session, result);

            log.LogDebug("Processed P2P trade set-money request from player {PlayerGuid}: credits {Credits}, result {Result}.",
                session.Player?.Guid, setMoney.Credits, result?.ToString() ?? "Updated");
        }
    }

    internal static class TradeRequestHelper
    {
        public static IItem GetInventoryItem(IWorldSession session, ulong itemGuid)
        {
            IItem item = session.Player.Inventory.GetItem(itemGuid);
            if (item == null)
                throw new InvalidPacketValueException();

            return item;
        }

        public static void SendTradeResult(IWorldSession session, ServerP2PTradeResult.P2PTradeResult? result)
        {
            if (!result.HasValue)
                return;

            session.EnqueueMessageEncrypted(new ServerP2PTradeResult
            {
                Result = result.Value
            });
        }
    }
}
