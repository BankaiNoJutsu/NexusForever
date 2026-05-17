using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Guild;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Guild;

namespace NexusForever.WorldServer.Network.Message.Handler.Guild
{
    public class ClientGuildPerkActivateHandler : IMessageHandler<IWorldSession, ClientGuildPerkActivate>
    {
        #region Dependency Injection

        private readonly IGlobalGuildManager globalGuildManager;

        public ClientGuildPerkActivateHandler(IGlobalGuildManager globalGuildManager)
        {
            this.globalGuildManager = globalGuildManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientGuildPerkActivate guildPerkActivate)
        {
            if (guildPerkActivate.Operation != GuildOperation.ActivatePerk)
                throw new InvalidPacketValueException($"Invalid guild perk activate operation received: {guildPerkActivate.Operation}");

            globalGuildManager.HandleGuildOperation(session.Player, guildPerkActivate);
        }
    }
}
