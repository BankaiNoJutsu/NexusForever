using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientSpline2RequestHandler : IMessageHandler<IWorldSession, ClientSpline2Request>
    {
        public void HandleMessage(IWorldSession session, ClientSpline2Request message)
        {
            // TODO: implement spline2 request handling
        }
    }
}
