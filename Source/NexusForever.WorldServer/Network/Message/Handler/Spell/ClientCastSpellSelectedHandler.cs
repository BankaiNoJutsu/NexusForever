using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientCastSpellSelectedHandler : IMessageHandler<IWorldSession, ClientCastSpellSelected>
    {
        private readonly ILogger<ClientCastSpellSelectedHandler> log;

        public ClientCastSpellSelectedHandler(ILogger<ClientCastSpellSelectedHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCastSpellSelected castSpellSelected)
        {
            log.LogDebug("Ignoring unsupported selected spell-cast request from player {PlayerGuid}: selected entry {SelectedEntryId}, target {TargetEntityId}, context token {ContextToken}, position {Position}.",
                session.Player?.Guid,
                castSpellSelected.SelectedEntryId,
                castSpellSelected.TargetEntityId,
                castSpellSelected.ContextToken,
                castSpellSelected.Position);
        }
    }
}
