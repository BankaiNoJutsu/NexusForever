using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientCancelEffectHandler : IMessageHandler<IWorldSession, ClientCancelEffect>
    {
        public void HandleMessage(IWorldSession session, ClientCancelEffect cancelSpell)
        {
            session.Player.TryCancelSpellEffect(cancelSpell.ServerUniqueId);
        }
    }
}
