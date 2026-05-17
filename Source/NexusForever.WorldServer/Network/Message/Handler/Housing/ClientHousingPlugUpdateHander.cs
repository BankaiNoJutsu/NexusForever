using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Housing
{
    public class ClientHousingPlugUpdateHander : IMessageHandler<IWorldSession, ClientHousingPlugUpdate>
    {
        public void HandleMessage(IWorldSession session, ClientHousingPlugUpdate housingPlugUpdate)
        {
            if (session.Player.Map is not IResidenceMapInstance residenceMap)
                throw new InvalidPacketValueException();

            residenceMap.PlugUpdate(session.Player, housingPlugUpdate);
        }
    }
}
