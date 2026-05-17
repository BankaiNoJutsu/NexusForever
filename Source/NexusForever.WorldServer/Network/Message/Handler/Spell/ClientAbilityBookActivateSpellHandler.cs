using Microsoft.Extensions.Logging;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientAbilityBookActivateSpellHandler : IMessageHandler<IWorldSession, ClientAbilityBookActivateSpell>
    {
        private readonly ILogger<ClientAbilityBookActivateSpellHandler> log;

        public ClientAbilityBookActivateSpellHandler(ILogger<ClientAbilityBookActivateSpellHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientAbilityBookActivateSpell activateSpell)
        {
            if (!session.Player.SpellManager.SetSpellActivation(activateSpell.Spell4Id, activateSpell.Active))
            {
                log.LogWarning("Rejecting ability book activation change from player {Player}: spell4Id={Spell4Id}, active={Active}",
                    session.Player?.Guid, activateSpell.Spell4Id, activateSpell.Active);
                return;
            }

            log.LogDebug("Updated ability book activation for player {Player}: spell4Id={Spell4Id}, active={Active}",
                session.Player?.Guid, activateSpell.Spell4Id, activateSpell.Active);
        }
    }
}
