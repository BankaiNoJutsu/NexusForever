using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.PublicEvent;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientPublicEventRequestScoreboardHandler : IMessageHandler<IWorldSession, ClientPublicEventRequestScoreboard>
    {
        private readonly ILogger<ClientPublicEventRequestScoreboardHandler> log;

        public ClientPublicEventRequestScoreboardHandler(ILogger<ClientPublicEventRequestScoreboardHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPublicEventRequestScoreboard requestScoreboard)
        {
            log.LogDebug("ClientPublicEventRequestScoreboard: player={Player} eventId={EventId} subscribe={Subscribe}",
                session.Player?.Guid, requestScoreboard.PublicEventId, requestScoreboard.Subscribe);

            if (!requestScoreboard.Subscribe)
                return;

            // WildStar64.exe scoreboard request 0x06FA carries publicEventId u14
            // plus a subscribe bit. Until subscription cadence and reward result
            // ordering are captured, respond with one participant-scoped stats
            // snapshot and leave reward delivery to mapped event-end paths.
            IPublicEvent publicEvent = session.Player?.Map?.PublicEventManager.GetEvent(requestScoreboard.PublicEventId);
            publicEvent?.SendScoreboardUpdate(session.Player);
        }
    }
}
