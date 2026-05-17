using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Fortune;

namespace NexusForever.WorldServer.Network.Message.Handler.Fortune
{
    public class ClientFortuneNotifyGameHandler : IMessageHandler<IWorldSession, ClientFortuneNotifyGame>
    {
        public void HandleMessage(IWorldSession session, ClientFortuneNotifyGame message)
        {
            // TODO: implement fortune game notification
        }
    }
}
