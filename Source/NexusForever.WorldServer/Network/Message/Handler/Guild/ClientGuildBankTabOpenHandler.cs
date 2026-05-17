using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientGuildBankTabOpenHandler : IMessageHandler<IWorldSession, ClientGuildBankTabOpen>
    {
        private readonly ILogger<ClientGuildBankTabOpenHandler> log;

        public ClientGuildBankTabOpenHandler(ILogger<ClientGuildBankTabOpenHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientGuildBankTabOpen bankTabOpen)
        {
            log.LogDebug("ClientGuildBankTabOpen: player={Player} tabIndex={TabIndex}",
                session.Player?.Guid, bankTabOpen.BankTabIndex);
        }
    }
}
