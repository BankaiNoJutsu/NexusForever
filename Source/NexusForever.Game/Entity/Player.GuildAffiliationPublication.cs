using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Guild;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        internal bool OwnsGuildAffiliationPublication(GuildManager manager, Identity identity, IGameSession session,
            EventQueue events, IAccount account, ulong epoch)
            => Identity?.Id == identity.Id && Identity.RealmId == identity.RealmId
                && CharacterId == identity.Id && ReferenceEquals(GuildManager, manager)
                && ReferenceEquals(Session, session) && ReferenceEquals(session?.Events, events)
                && ReferenceEquals(Account, account) && (account?.WorldSessionEpoch ?? 0) == epoch
                && (playerManager == null || ReferenceEquals(playerManager.GetPlayer(identity), this))
                && session is IPlayerSession current && ReferenceEquals(current.Player, this)
                && session is not NetworkSession { IsDisconnecting: true };

        internal IPlayer[] CaptureGuildAffiliationRecipients()
            => (!IsLoading ? new IPlayer[] { this } : []).Concat(visibleEntities.Values.OfType<IPlayer>()
                .Where(recipient => IsLoading || recipient.Guid != Guid)).ToArray();
    }
}
