using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Achievement;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientSteamAchievementsHandler : IMessageHandler<IWorldSession, ClientSteamAchievements>
    {
        public void HandleMessage(IWorldSession session, ClientSteamAchievements message)
        {
            // TODO: handle Steam achievement sync from client
        }
    }
}
