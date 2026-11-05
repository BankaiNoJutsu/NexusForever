using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Spell;
using NexusForever.Network.Message;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientCastSpellHandler : IMessageHandler<IWorldSession, ClientCastSpell>
    {
        /// <summary>
        /// This Handler is ued when a player has disabled continuous casting in Settings > Controls
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientCastSpell castSpell)
        {
            ICharacterSpell characterSpell = GetCharacterSpell(session, castSpell.BagIndex, castSpell.ContextToken);
            if (characterSpell == null)
                return;
            CastCharacterSpell(session, characterSpell, castSpell.PrimaryTargetId, null, castSpell.ContextToken, nameof(ClientCastSpell));
        }

        internal static ICharacterSpell GetCharacterSpell(IWorldSession session, ushort bagIndex, uint contextToken)
        {
            IPlayer player = session.Player;
            if (player == null)
                return null;
            IItem item = player.Inventory.GetItem(InventoryLocation.Ability, bagIndex);
            ICharacterSpell characterSpell = item == null ? null : player.SpellManager.GetSpell(item.Id);
            if (!ReferenceEquals(session.Player, player) || !ReferenceEquals(player.Session, session))
                return null;

            // A parsed request can outlive its action-bar slot or learned entry.
            // Its explicit request presence also owns a wrapped token of zero.
            if (characterSpell == null)
                ClientCastRequest.SendFailure(player, true, contextToken, 0, CastResult.SpellUnknown);

            return characterSpell;
        }

        internal static void CastCharacterSpell(IWorldSession session, ICharacterSpell characterSpell, uint primaryTargetId = 0u, Position position = null, uint clientContextToken = 0u, string clientRequestSource = null)
        {
            var spellParameters = new SpellParameters
            {
                CharacterSpell         = characterSpell,
                SpellInfo              = characterSpell.GetSpellInfoForCast(),
                RootSpellInfo          = characterSpell.SpellInfo,
                PrimaryTargetId        = primaryTargetId,
                Position               = position,
                UserInitiatedSpellCast = true,
                ClientContextToken     = clientContextToken,
                HasClientCastRequest   = clientRequestSource != null,
                ClientRequestSource    = clientRequestSource
            };

            ClientSpellEvidenceCaptureHelper.ApplyPendingCapture(session, spellParameters);
            CastResult castResult = session.Player.TryCastSpell(spellParameters.SpellInfo.Entry.Id, spellParameters);
            characterSpell.CompleteSpellInfoCast(spellParameters.SpellInfo, castResult);
        }
    }
}
