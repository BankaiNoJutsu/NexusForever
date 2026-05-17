using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pregame;

namespace NexusForever.WorldServer.Network.Message.Handler.Character
{
    public class ClientCharacterRenameHandler : IMessageHandler<IWorldSession, ClientCharacterRename>
    {
        private readonly ILogger<ClientCharacterRenameHandler> log;

        public ClientCharacterRenameHandler(ILogger<ClientCharacterRenameHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCharacterRename characterRename)
        {
            log.LogDebug("ClientCharacterRename: player={Player} characterId={CharacterId} name={Name}",
                session.Player?.Guid, characterRename.CharacterId, characterRename.Name);
        }
    }
}
