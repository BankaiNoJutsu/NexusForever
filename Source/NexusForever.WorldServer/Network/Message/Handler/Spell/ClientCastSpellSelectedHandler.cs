using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientCastSpellSelectedHandler : IMessageHandler<IWorldSession, ClientCastSpellSelected>
    {
        private readonly ILogger<ClientCastSpellSelectedHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientCastSpellSelectedHandler(ILogger<ClientCastSpellSelectedHandler> log, IGameTableManager gameTableManager = null)
        {
            this.log = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientCastSpellSelected castSpellSelected)
        {
            ICharacterSpell characterSpell = session.Player.SpellManager.GetSpellForSpell4Id(castSpellSelected.SelectedEntryId);
            if (characterSpell == null || characterSpell.Tier == 0)
            {
                if (session.Player.SpellManager.IsActiveFloatingActionBarSpell(castSpellSelected.SelectedEntryId))
                {
                    CastSelectedSpell(session, castSpellSelected, castSpellSelected.SelectedEntryId);
                    return;
                }

                if (session.Player.SpellManager.TryResolveActivePetActionSpell(castSpellSelected.SelectedEntryId, out uint petActionSpell4Id))
                {
                    if (session.Player.SpellManager.GetSpellCooldown(castSpellSelected.SelectedEntryId) > 0d)
                    {
                        session.EnqueueMessageEncrypted(new ServerSpellCastResult
                        {
                            ContextToken = castSpellSelected.ContextToken,
                            Spell4Id     = castSpellSelected.SelectedEntryId,
                            CastResult   = CastResult.SpellCooldown
                        });
                        return;
                    }

                    CastResult castResult = CastSelectedSpell(session, castSpellSelected, petActionSpell4Id);
                    if (castResult == CastResult.Ok)
                        ApplySelectedSpellCooldown(session, castSpellSelected.SelectedEntryId, petActionSpell4Id);

                    return;
                }

                log.LogWarning("Rejecting selected spell-cast request from player {PlayerGuid}: selected entry {SelectedEntryId}, target {TargetEntityId}, context token {ContextToken}.",
                    session.Player?.Guid,
                    castSpellSelected.SelectedEntryId,
                    castSpellSelected.TargetEntityId,
                    castSpellSelected.ContextToken);

                session.EnqueueMessageEncrypted(new ServerSpellCastResult
                {
                    ContextToken = castSpellSelected.ContextToken,
                    Spell4Id     = castSpellSelected.SelectedEntryId,
                    CastResult   = CastResult.SpellUnknown
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

        private static CastResult CastSelectedSpell(IWorldSession session, ClientCastSpellSelected castSpellSelected, uint spell4Id)
        {
            var spellParameters = new SpellParameters
            {
                PrimaryTargetId        = castSpellSelected.TargetEntityId,
                Position               = castSpellSelected.Position,
                UserInitiatedSpellCast = true,
                ClientContextToken     = castSpellSelected.ContextToken,
                ClientRequestSource    = nameof(ClientCastSpellSelected)
            };

            ClientSpellEvidenceCaptureHelper.ApplyPendingCapture(session, spellParameters);
            return session.Player.TryCastSpell(spell4Id, spellParameters);
        }

        private void ApplySelectedSpellCooldown(IWorldSession session, uint selectedSpell4Id, uint castSpell4Id)
        {
            if (selectedSpell4Id == castSpell4Id)
                return;

            Spell4Entry selectedEntry = gameTableManager?.Spell4?.GetEntry(selectedSpell4Id);
            if (selectedEntry == null || selectedEntry.SpellCoolDown == 0u)
                return;

            session.Player.SpellManager.SetSpellCooldown(selectedSpell4Id, selectedEntry.SpellCoolDown / 1000d);
        }
    }
}
