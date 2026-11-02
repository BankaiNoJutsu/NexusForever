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
        /// Validate the client communicator report before recording any quest mention.
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientCommunicatorAction communicatorAction)
        {
            if (session.Player == null || !ReferenceEquals(session.Player.Account, session.Account)
                || !ReferenceEquals(session.Player.Session, session)) return;
            if (communicatorAction.QuestId <= ushort.MaxValue)
                session.Player?.QuestManager?.QuestCommunicatorAction((ushort)communicatorAction.QuestId, communicatorAction.Engaged);
            log.LogDebug("ClientCommunicatorAction: player={Player}, questId={QuestId}, engaged={Engaged}",
                session.Player?.Guid, communicatorAction.QuestId, communicatorAction.Engaged);
        }
    }
}
