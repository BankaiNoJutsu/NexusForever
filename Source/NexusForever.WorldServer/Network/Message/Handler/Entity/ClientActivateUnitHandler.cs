using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Trade;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientActivateUnitHandler : IMessageHandler<IWorldSession, ClientActivateUnit>
    {
        private readonly ITradeManager tradeManager;

        public ClientActivateUnitHandler(ITradeManager tradeManager)
        {
            this.tradeManager = tradeManager;
        }

        public void HandleMessage(IWorldSession session, ClientActivateUnit activateUnit)
        {
            IWorldEntity entity = session.Player.GetVisible<IWorldEntity>(activateUnit.ActivateUnitId);
            if (entity == null)
                throw new InvalidPacketValueException();

            if (ActivationInteractionGuards.TryRejectBusyTarget(session, entity))
                return;

            if (ActivationInteractionGuards.TryRejectOutOfRangeTarget(session, entity))
                return;

            entity.OnActivate(session.Player);
            tradeManager.Cancel(session.Player);
            entity.OnActivateSuccess(session.Player);
        }
    }
}
