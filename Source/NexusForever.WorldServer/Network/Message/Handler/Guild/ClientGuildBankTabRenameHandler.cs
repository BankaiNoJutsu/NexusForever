using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientGuildBankTabRenameHandler : IMessageHandler<IWorldSession, ClientGuildBankTabRename>
    {
        #region Dependency Injection

        private readonly IGlobalGuildManager globalGuildManager;

        public ClientGuildBankTabRenameHandler(IGlobalGuildManager globalGuildManager)
        {
            this.globalGuildManager = globalGuildManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientGuildBankTabRename guildBankTabRename)
        {
            if (guildBankTabRename.Operation != GuildOperation.BankTabRename)
                throw new InvalidPacketValueException($"Invalid guild bank tab rename operation received: {guildBankTabRename.Operation}");

            globalGuildManager.HandleGuildOperation(session.Player, guildBankTabRename);
        }
    }
}
