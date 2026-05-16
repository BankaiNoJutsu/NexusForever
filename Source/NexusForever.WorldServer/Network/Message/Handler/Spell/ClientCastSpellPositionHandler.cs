using NexusForever.Game.Abstract.Spell;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientCastSpellPositionHandler : IMessageHandler<IWorldSession, ClientCastSpellPosition>
    {
        public void HandleMessage(IWorldSession session, ClientCastSpellPosition castSpell)
        {
            ICharacterSpell characterSpell = ClientCastSpellHandler.GetCharacterSpell(session, castSpell.BagIndex);
            ClientCastSpellHandler.CastCharacterSpell(session, characterSpell, position: castSpell.Position, clientContextToken: castSpell.ContextToken, clientRequestSource: nameof(ClientCastSpellPosition));
        }
    }
}
