using NexusForever.Game.Static.Account;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.CREDDExchange;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Account
{
    public interface ICREDDExchangeService
    {
        ServerCREDDExchangeInfoResults BuildInfoResults();
        ServerCREDDExchangeOrderCacheRows BuildOrderCacheRows();
        ServerCREDDOperationHistory BuildOperationHistory(IWorldSession session);
        AccountOperation SubmitBuyOrder(IWorldSession session, ulong creditAmount, bool submitFlag, out AccountOperationResult result);
        AccountOperation SubmitSellOrder(IWorldSession session, ulong creditAmount, bool submitFlag, out AccountOperationResult result);
        AccountOperationResult CancelOrder(IWorldSession session, ulong orderId);
    }
}
