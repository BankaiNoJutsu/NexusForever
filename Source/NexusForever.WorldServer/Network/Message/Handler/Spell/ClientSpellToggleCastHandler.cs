using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientSpellToggleCastHandler : IMessageHandler<IWorldSession, ClientSpellToggleCast>
    {
        private readonly ILogger<ClientSpellToggleCastHandler> log;

        public ClientSpellToggleCastHandler(ILogger<ClientSpellToggleCastHandler> log)
        {
            this.log = log;
        }

        /// <summary>
        /// Client reports whether continuous spell-cast mode is enabled. The server tracks this independently; this is diagnostic only.
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientSpellToggleCast toggleCast)
        {
            log.LogDebug("ClientSpellToggleCast: player={Player}, enabled={Enabled}",
                session.Player?.Guid, toggleCast.Enabled);
        }
    }
}
