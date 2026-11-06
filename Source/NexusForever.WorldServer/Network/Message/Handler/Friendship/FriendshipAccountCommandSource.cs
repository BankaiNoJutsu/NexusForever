using NexusForever.Game;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Internal.Message.Friendship.Shared;
using NexusForever.Network.Session;

namespace NexusForever.WorldServer.Network.Message.Handler.Friendship
{
    internal static class FriendshipAccountCommandSource
    {
        internal static bool TryCapture(IWorldSession session, out IPlayer player, out IAccount account,
            out FriendshipAccountCommandOwner owner)
        {
            player = session?.Player; account = session?.Account; owner = null;
            if (player == null || account == null || account.Id == 0 || player.CharacterId == 0
                || player.Identity?.RealmId is null or 0 || player.Identity.Id != player.CharacterId
                || !ReferenceEquals(player.Session, session) || !ReferenceEquals(player.Account, account)
                || !ReferenceEquals(account.Session, session) || session is NetworkSession { IsDisconnecting: true }
                || player.PresenceSessionEpoch == 0 || player.PresenceAdmission == 0
                || player.PresenceSessionEpoch != account.WorldSessionEpoch) return false;
            owner = new() { Identity = player.Identity.ToInternalIdentity(),
                SessionEpoch = player.PresenceSessionEpoch, Admission = player.PresenceAdmission };
            return true;
        }
    }
}
