using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Spell;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientCastGuildBossTokenHandler : IMessageHandler<IWorldSession, ClientCastGuildBossToken>
    {
        public void HandleMessage(IWorldSession session, ClientCastGuildBossToken message)
        {
            // TODO: implement guild boss token cast
        }
    }
}
