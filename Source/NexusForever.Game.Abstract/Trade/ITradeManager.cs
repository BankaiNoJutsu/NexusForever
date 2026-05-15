using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Abstract.Trade
{
    public interface ITradeManager : IUpdate
    {
        ServerP2PTradeResult.P2PTradeResult? Initiate(IPlayer initiator, IPlayer target);
        ServerP2PTradeResult.P2PTradeResult? Accept(IPlayer player);
        ServerP2PTradeResult.P2PTradeResult? Decline(IPlayer player);
        ServerP2PTradeResult.P2PTradeResult? Cancel(IPlayer player);
        ServerP2PTradeResult.P2PTradeResult? AddItem(IPlayer player, IItem item);
        ServerP2PTradeResult.P2PTradeResult? RemoveItem(IPlayer player, ulong itemGuid);
        ServerP2PTradeResult.P2PTradeResult? SetMoney(IPlayer player, ulong credits);
        ServerP2PTradeResult.P2PTradeResult? Commit(IPlayer player);
    }
}
