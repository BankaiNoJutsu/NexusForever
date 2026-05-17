using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientGuildBankTransaction2Handler : IMessageHandler<IWorldSession, ClientGuildBankTransaction2>
    {
        private readonly ILogger<ClientGuildBankTransaction2Handler> log;

        public ClientGuildBankTransaction2Handler(ILogger<ClientGuildBankTransaction2Handler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientGuildBankTransaction2 bankTransaction2)
        {
            log.LogDebug("ClientGuildBankTransaction2: player={Player} srcGuid={SrcGuid} dstGuid={DstGuid} count={Count} freeSlot={FreeSlot}",
                session.Player?.Guid, bankTransaction2.SourceItemGuid, bankTransaction2.DestinationItemGuid,
                bankTransaction2.Count, bankTransaction2.UseFirstFreeSlot);
        }
    }
}
