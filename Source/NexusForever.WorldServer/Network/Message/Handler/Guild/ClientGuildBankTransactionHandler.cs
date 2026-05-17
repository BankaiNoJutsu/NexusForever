using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientGuildBankTransactionHandler : IMessageHandler<IWorldSession, ClientGuildBankTransaction>
    {
        private readonly ILogger<ClientGuildBankTransactionHandler> log;

        public ClientGuildBankTransactionHandler(ILogger<ClientGuildBankTransactionHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientGuildBankTransaction bankTransaction)
        {
            log.LogDebug("ClientGuildBankTransaction: player={Player} srcGuid={SrcGuid} dstGuid={DstGuid} count={Count}",
                session.Player?.Guid, bankTransaction.SourceItemGuid, bankTransaction.DestinationItemGuid, bankTransaction.Count);
        }
    }
}
