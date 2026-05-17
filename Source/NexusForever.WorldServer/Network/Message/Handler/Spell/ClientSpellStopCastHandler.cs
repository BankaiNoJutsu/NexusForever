using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientSpellStopCastHandler : IMessageHandler<IWorldSession, ClientSpellStopCast>
    {
        public void HandleMessage(IWorldSession session, ClientSpellStopCast spellStopCast)
        {
            if (spellStopCast.CastResult is not (CastResult.SpellCancelled or CastResult.SpellInterrupted))
                throw new InvalidPacketValueException($"Invalid spell stop cast result received: {spellStopCast.CastResult}");

            session.Player.CancelSpellCast(spellStopCast.CastingId, spellStopCast.CastResult);
        }
    }
}
