using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Session;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        internal bool OwnsArchivePublication(GalacticArchiveManager manager, Identity identity, IGameSession session)
            => Identity?.Id == identity.Id && Identity.RealmId == identity.RealmId
                && ReferenceEquals(GalacticArchiveManager, manager) && ReferenceEquals(Session, session)
                && (playerManager == null || ReferenceEquals(playerManager.GetPlayer(identity), this))
                && session is IPlayerSession current && ReferenceEquals(current.Player, this)
                && session is not NetworkSession { IsDisconnecting: true };
    }
}
