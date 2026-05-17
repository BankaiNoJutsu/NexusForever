using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientWarPartyBossTokensRequestHandler : IMessageHandler<IWorldSession, ClientWarPartyBossTokensRequest>
    {
        public void HandleMessage(IWorldSession session, ClientWarPartyBossTokensRequest message)
        {
            // TODO: implement war party boss tokens request
        }
    }
}
