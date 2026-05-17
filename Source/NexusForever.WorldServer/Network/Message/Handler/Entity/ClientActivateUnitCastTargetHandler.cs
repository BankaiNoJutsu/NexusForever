using NexusForever.Game.Abstract;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientActivateUnitCastTargetHandler : IMessageHandler<IWorldSession, ClientActivateUnitCastTarget>
    {
        private readonly ClientActivateUnitCastHandler activateUnitCastHandler;

        public ClientActivateUnitCastTargetHandler(ClientActivateUnitCastHandler activateUnitCastHandler)
        {
            this.activateUnitCastHandler = activateUnitCastHandler;
        }

        public void HandleMessage(IWorldSession session, ClientActivateUnitCastTarget activateUnitCastTarget)
        {
            activateUnitCastHandler.HandleMessageInternal(session, activateUnitCastTarget.TargetField, activateUnitCastTarget.ContextToken, nameof(ClientActivateUnitCastTarget));
        }
    }
}