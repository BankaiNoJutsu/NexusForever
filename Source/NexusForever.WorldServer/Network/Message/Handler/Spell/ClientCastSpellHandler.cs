using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Spell;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientCastSpellHandler : IMessageHandler<IWorldSession, ClientCastSpell>
    {
        /// <summary>
        /// This Handler is ued when a player has disabled continuous casting in Settings > Controls
        /// </summary>
        public void HandleMessage(IWorldSession session, ClientCastSpell castSpell)
        {
            ICharacterSpell characterSpell = GetCharacterSpell(session, castSpell.BagIndex);
            CastCharacterSpell(session, characterSpell, castSpell.PrimaryTargetId);
        }

        internal static ICharacterSpell GetCharacterSpell(IWorldSession session, ushort bagIndex)
        {
            IItem item = session.Player.Inventory.GetItem(InventoryLocation.Ability, bagIndex);
            if (item == null)
                throw new InvalidPacketValueException();

            ICharacterSpell characterSpell = session.Player.SpellManager.GetSpell(item.Id);
            if (characterSpell == null)
                throw new InvalidPacketValueException();

            return characterSpell;
        }

        internal static void CastCharacterSpell(IWorldSession session, ICharacterSpell characterSpell, uint primaryTargetId = 0u, Position position = null)
        {
            session.Player.CastSpell(new SpellParameters
            {
                CharacterSpell         = characterSpell,
                SpellInfo              = characterSpell.SpellInfo,
                PrimaryTargetId        = primaryTargetId,
                Position               = position,
                UserInitiatedSpellCast = true
            });
        }
    }
}
