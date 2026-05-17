using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientGuildBankMoneyTransactionHandler : IMessageHandler<IWorldSession, ClientGuildBankMoneyTransaction>
    {
        private readonly ILogger<ClientGuildBankMoneyTransactionHandler> log;

        public ClientGuildBankMoneyTransactionHandler(ILogger<ClientGuildBankMoneyTransactionHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientGuildBankMoneyTransaction moneyTransaction)
        {
            log.LogDebug("ClientGuildBankMoneyTransaction: player={Player} amount={Amount}",
                session.Player?.Guid, moneyTransaction.Amount);
        }
    }
}
