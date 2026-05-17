using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientSpellCastStateHandler : IMessageHandler<IWorldSession, ClientSpellCastState>
    {
        private readonly ILogger<ClientSpellCastStateHandler> log;

        public ClientSpellCastStateHandler(ILogger<ClientSpellCastStateHandler> log)
        {
            this.log = log;
        }

        /// <summary>
        /// Client reports a one-bit spell cast gating state. The server uses its own authoritative state; this is diagnostic only.
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientSpellCastState castState)
        {
            log.LogDebug("ClientSpellCastState: player={Player}, state={State}",
                session.Player?.Guid, castState.State);
        }
    }
}
