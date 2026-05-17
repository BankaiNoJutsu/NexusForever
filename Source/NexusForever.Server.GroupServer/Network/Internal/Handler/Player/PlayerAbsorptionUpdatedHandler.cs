using NexusForever.Database.Group;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Server.GroupServer.Character;
using Rebus.Handlers;

namespace NexusForever.Server.GroupServer.Network.Internal.Handler.Player
{
    public class PlayerAbsorptionUpdatedHandler : IHandleMessages<PlayerAbsorptionUpdatedMessage>
    {
        #region Dependency Injection

        private readonly GroupContext _groupContext;
        private readonly CharacterManager _characterManager;

        public PlayerAbsorptionUpdatedHandler(
            GroupContext groupContext,
            CharacterManager characterManager)
        {
            _groupContext     = groupContext;
            _characterManager = characterManager;
        }

        #endregion

        public async Task Handle(PlayerAbsorptionUpdatedMessage message)
        {
            var character = await _characterManager.GetCharacterAsync(message.Identity.ToGroupIdentity());
            if (character == null)
                return;

            character.Absorption       = message.Absorption;
            character.MaxAbsorption    = message.AbsorptionMax;
            character.HealingAbsorb    = message.HealingAbsorb;
            character.MaxHealingAbsorb = message.HealingAbsorbMax;

            await _groupContext.SaveChangesAsync();
        }
    }
}
