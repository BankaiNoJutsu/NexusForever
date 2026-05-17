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

        /// <summary>
        /// Logs unsupported ability book activation changes.
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientAbilityBookActivateSpell activateSpell)
        {
            log.LogDebug("Ignoring unsupported ClientAbilityBookActivateSpell: player={Player}, spell4Id={Spell4Id}, active={Active}",
                session.Player?.Guid, activateSpell.Spell4Id, activateSpell.Active);
        }
    }
}
