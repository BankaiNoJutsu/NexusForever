using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.Game.Spell
{
    public static class ClientCastRequest
    {
        // Request presence is explicit: the native pending-token counter can wrap
        // to zero. Numeric tokens retained by server work do not own a request.
        public static void SendFailure(IPlayer player, bool hasRequest, uint contextToken, uint spell4Id, CastResult result)
        {
            if (!hasRequest || player == null || player.IsLoading || result == CastResult.Ok)
                return;
            player.Session?.EnqueueMessageEncrypted(new ServerSpellCastResult
            {
                ContextToken = contextToken,
                Spell4Id = spell4Id,
                CastResult = result
            });
        }
    }
}
