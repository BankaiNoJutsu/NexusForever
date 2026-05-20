using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Pregame;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientPingHandler : IMessageHandler<IWorldSession, ClientPregameKeepAlive>
    {
        public void HandleMessage(IWorldSession session, ClientPregameKeepAlive ping)
        {
            session.Heartbeat.OnHeartbeat();
        }
    }

    public class ClientStateHeartbeatHandler : IMessageHandler<IWorldSession, State>
    {
        public void HandleMessage(IWorldSession session, State state)
        {
            session.Heartbeat.OnHeartbeat();
        }
    }

    public class ClientState2HeartbeatHandler : IMessageHandler<IWorldSession, State2>
    {
        public void HandleMessage(IWorldSession session, State2 state)
        {
            session.Heartbeat.OnHeartbeat();
        }
    }
}
