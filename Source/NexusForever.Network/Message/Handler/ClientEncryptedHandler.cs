using NexusForever.Network.Message.Model;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;

using NexusForever.Shared.Diagnostics;

namespace NexusForever.Network.Message.Handler
{
    public class ClientEncryptedHandler : IMessageHandler<IGameSession, ClientEncrypted>
    {
        public void HandleMessage(IGameSession session, ClientEncrypted encrypted)
        {
            session.HandlePacket(new ClientGamePacket
            {
                Data        = encrypted.Data,
                IsEncrypted = true,
                QueuedTimestamp = NexusForeverDiagnostics.GetTimestamp()
            });
        }
    }
}
