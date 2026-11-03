using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Session;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        internal bool OwnsTitlePublication(TitleManager manager, Identity identity, IGameSession session)
            => Identity?.Id == identity.Id && Identity.RealmId == identity.RealmId && CharacterId == identity.Id
                && ReferenceEquals(TitleManager, manager) && ReferenceEquals(Session, session)
                && (playerManager == null || ReferenceEquals(playerManager.GetPlayer(identity), this))
                && session is IPlayerSession current && ReferenceEquals(current.Player, this)
                && session is not NetworkSession { IsDisconnecting: true };

        internal IPlayer[] CaptureTitleRecipients()
            => (!IsLoading ? new IPlayer[] { this } : []).Concat(visibleEntities.Values.OfType<IPlayer>()
                .Where(recipient => IsLoading || recipient.Guid != Guid)).ToArray();
    }
}
