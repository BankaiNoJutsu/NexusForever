using Microsoft.Extensions.Logging;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Achievement;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientSteamAchievementsHandler : IMessageHandler<IWorldSession, ClientSteamAchievements>
    {
        private readonly ILogger<ClientSteamAchievementsHandler> log;

        public ClientSteamAchievementsHandler(ILogger<ClientSteamAchievementsHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientSteamAchievements achievements)
        {
            log.LogDebug("ClientSteamAchievements: player={Player}, steamGameId={SteamGameId}, dataLength={DataLength}.",
                session.Player?.Guid, achievements.SteamGameId, achievements.AchievementData?.Length ?? 0);
        }
    }
}
