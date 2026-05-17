using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Quest
{
    public class ClientCommunicatorActionHandler : IMessageHandler<IWorldSession, ClientCommunicatorAction>
    {
        private readonly ILogger<ClientCommunicatorActionHandler> log;

        public ClientCommunicatorActionHandler(ILogger<ClientCommunicatorActionHandler> log)
        {
            this.log = log;
        }

        /// <summary>
        /// Client sends this when the player opens (Engaged=true) or dismisses (Engaged=false) the communicator for a quest.
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientCommunicatorAction communicatorAction)
        {
            log.LogDebug("ClientCommunicatorAction: player={Player}, questId={QuestId}, engaged={Engaged}",
                session.Player?.Guid, communicatorAction.QuestId, communicatorAction.Engaged);
        }
    }
}
