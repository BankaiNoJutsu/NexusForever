using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Trade
{
    public class ClientP2PTradingAcceptInviteHandler : IMessageHandler<IWorldSession, ClientP2PTradingAcceptInvite>
    {
        private readonly ILogger<ClientP2PTradingAcceptInviteHandler> log;

        public ClientP2PTradingAcceptInviteHandler(ILogger<ClientP2PTradingAcceptInviteHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingAcceptInvite acceptInvite)
        {
            log.LogDebug("Ignoring unsupported P2P trade accept-invite request from player {PlayerGuid}.",
                session.Player?.Guid);
        }
    }

    public class ClientP2PTradingDeclineInviteHandler : IMessageHandler<IWorldSession, ClientP2PTradingDeclineInvite>
    {
        private readonly ILogger<ClientP2PTradingDeclineInviteHandler> log;

        public ClientP2PTradingDeclineInviteHandler(ILogger<ClientP2PTradingDeclineInviteHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingDeclineInvite declineInvite)
        {
            log.LogDebug("Ignoring unsupported P2P trade decline-invite request from player {PlayerGuid}.",
                session.Player?.Guid);
        }
    }

    public class ClientP2PTradingCancelTradeHandler : IMessageHandler<IWorldSession, ClientP2PTradingCancelTrade>
    {
        private readonly ILogger<ClientP2PTradingCancelTradeHandler> log;

        public ClientP2PTradingCancelTradeHandler(ILogger<ClientP2PTradingCancelTradeHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingCancelTrade cancelTrade)
        {
            log.LogDebug("Ignoring unsupported P2P trade cancel request from player {PlayerGuid}.",
                session.Player?.Guid);
        }
    }

    public class ClientP2PTradingCommitHandler : IMessageHandler<IWorldSession, ClientP2PTradingCommit>
    {
        private readonly ILogger<ClientP2PTradingCommitHandler> log;

        public ClientP2PTradingCommitHandler(ILogger<ClientP2PTradingCommitHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingCommit commit)
        {
            log.LogDebug("Ignoring unsupported P2P trade commit request from player {PlayerGuid}.",
                session.Player?.Guid);
        }
    }

    public class ClientP2PTradingInitiateTradeHandler : IMessageHandler<IWorldSession, ClientP2PTradingInitiateTrade>
    {
        private readonly ILogger<ClientP2PTradingInitiateTradeHandler> log;

        public ClientP2PTradingInitiateTradeHandler(ILogger<ClientP2PTradingInitiateTradeHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingInitiateTrade initiateTrade)
        {
            if (initiateTrade.TargetUnitId == 0u || initiateTrade.TargetUnitId == session.Player.Guid)
                throw new InvalidPacketValueException();

            IPlayer target = session.Player.GetVisible<IPlayer>(initiateTrade.TargetUnitId);
            if (target == null)
                throw new InvalidPacketValueException();

            log.LogDebug("Ignoring unsupported P2P trade initiate request from player {PlayerGuid}: target {TargetUnitId}.",
                session.Player?.Guid, initiateTrade.TargetUnitId);
        }
    }

    public class ClientP2PTradingAddItemHandler : IMessageHandler<IWorldSession, ClientP2PTradingAddItem>
    {
        private readonly ILogger<ClientP2PTradingAddItemHandler> log;

        public ClientP2PTradingAddItemHandler(ILogger<ClientP2PTradingAddItemHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingAddItem addItem)
        {
            TradeRequestHelper.GetInventoryItem(session, addItem.ItemGuid);

            log.LogDebug("Ignoring unsupported P2P trade add-item request from player {PlayerGuid}: item {ItemGuid}.",
                session.Player?.Guid, addItem.ItemGuid);
        }
    }

    public class ClientP2PTradingRemoveItemHandler : IMessageHandler<IWorldSession, ClientP2PTradingRemoveItem>
    {
        private readonly ILogger<ClientP2PTradingRemoveItemHandler> log;

        public ClientP2PTradingRemoveItemHandler(ILogger<ClientP2PTradingRemoveItemHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingRemoveItem removeItem)
        {
            TradeRequestHelper.GetInventoryItem(session, removeItem.ItemGuid);

            log.LogDebug("Ignoring unsupported P2P trade remove-item request from player {PlayerGuid}: item {ItemGuid}.",
                session.Player?.Guid, removeItem.ItemGuid);
        }
    }

    public class ClientP2PTradingSetMoneyHandler : IMessageHandler<IWorldSession, ClientP2PTradingSetMoney>
    {
        private readonly ILogger<ClientP2PTradingSetMoneyHandler> log;

        public ClientP2PTradingSetMoneyHandler(ILogger<ClientP2PTradingSetMoneyHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientP2PTradingSetMoney setMoney)
        {
            if (!session.Player.CurrencyManager.CanAfford(CurrencyType.Credits, setMoney.Credits))
                throw new InvalidPacketValueException();

            log.LogDebug("Ignoring unsupported P2P trade set-money request from player {PlayerGuid}: credits {Credits}.",
                session.Player?.Guid, setMoney.Credits);
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
    }
}
