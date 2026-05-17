using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Instance;

namespace NexusForever.WorldServer.Network.Message.Handler.Instance
{
    public class ClientResetInstancesHandler : IMessageHandler<IWorldSession, ClientResetInstances>
    {
        public void HandleMessage(IWorldSession session, ClientResetInstances message)
        {
            // TODO: implement instance reset
        }
    }
}
