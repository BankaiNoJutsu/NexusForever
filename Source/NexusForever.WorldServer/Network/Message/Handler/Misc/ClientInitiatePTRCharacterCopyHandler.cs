using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pregame;

namespace NexusForever.WorldServer.Network.Message.Handler.Misc
{
    public class ClientInitiatePTRCharacterCopyHandler : IMessageHandler<IWorldSession, ClientInitiatePTRCharacterCopy>
    {
        private readonly ILogger<ClientInitiatePTRCharacterCopyHandler> log;

        public ClientInitiatePTRCharacterCopyHandler(ILogger<ClientInitiatePTRCharacterCopyHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientInitiatePTRCharacterCopy message)
        {
            log.LogDebug("ClientInitiatePTRCharacterCopy: player={PlayerGuid} characterId={CharacterId}.",
                session.Player?.Guid, message.CharacterId);
        }
    }
}
