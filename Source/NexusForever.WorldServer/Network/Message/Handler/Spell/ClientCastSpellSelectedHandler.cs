using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientCastSpellSelectedHandler : IMessageHandler<IWorldSession, ClientCastSpellSelected>
    {
        private readonly ILogger<ClientCastSpellSelectedHandler> log;

        public ClientCastSpellSelectedHandler(ILogger<ClientCastSpellSelectedHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientCastSpellSelected castSpellSelected)
        {
            ICharacterSpell characterSpell = session.Player.SpellManager.GetSpellForSpell4Id(castSpellSelected.SelectedEntryId);
            if (characterSpell == null || characterSpell.Tier == 0)
            {
                log.LogWarning("Rejecting selected spell-cast request from player {PlayerGuid}: selected entry {SelectedEntryId}, target {TargetEntityId}, context token {ContextToken}.",
                    session.Player?.Guid,
                    castSpellSelected.SelectedEntryId,
                    castSpellSelected.TargetEntityId,
                    castSpellSelected.ContextToken);

                session.EnqueueMessageEncrypted(new ServerSpellCastResult
                {
                    Spell4Id   = castSpellSelected.SelectedEntryId,
                    CastResult = CastResult.SpellUnknown
                });
                return;
            }

            ClientCastSpellHandler.CastCharacterSpell(session,
                characterSpell,
                castSpellSelected.TargetEntityId,
                castSpellSelected.Position,
                castSpellSelected.ContextToken,
                nameof(ClientCastSpellSelected));
        }
    }
}
