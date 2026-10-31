using NexusForever.Database.Query;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Server.Character.Game.Character;
using Rebus.Handlers;

namespace NexusForever.Server.Character.Network.Internal.Handler.Player
{
    public class PlayerGuildAssociationUpdatedHandler : IHandleMessages<PlayerGuildAssociationUpdatedMessage>
    {
        #region Dependency Injection

        private readonly QueryContext _context;
        private readonly CharacterManager _characterManager;

        public PlayerGuildAssociationUpdatedHandler(
            QueryContext context,
            CharacterManager characterManager)
        {
            _context          = context;
            _characterManager = characterManager;
        }

        #endregion

        public async Task Handle(PlayerGuildAssociationUpdatedMessage message)
        {
            await _characterManager.UpdateCharacterRemoteAsync(message.Identity.ToQueryIdentity(),
                c => c.GuildName, message.GuildName);
        }
    }
}
