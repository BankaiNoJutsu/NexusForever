using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Game.Static.Spell;
using System.Linq;

namespace NexusForever.WorldServer.Network.Message.Handler.Pet
{
    public class ClientSummonVanityPetHandler : IMessageHandler<IWorldSession, ClientSummonVanityPet>
    {
        public void HandleMessage(IWorldSession session, ClientSummonVanityPet summonVanityPet)
        {
            ICharacterSpell spell = session.Player.SpellManager.GetSpell(summonVanityPet.Spell4BaseId);
            if (spell == null)
                throw new InvalidPacketValueException();

            byte tier = session.Player.SpellManager.GetSpellTier(spell.BaseInfo.Entry.Id);
            ISpellInfo spellInfo = spell.BaseInfo.GetSpellInfo(tier);
            if (spellInfo?.Effects?.Any(e => e.EffectType == SpellEffectType.SummonVanityPet) != true)
                throw new InvalidPacketValueException();

            session.Player.CastSpell(new SpellParameters
            {
                SpellInfo = spellInfo
            });
        }
    }
}
