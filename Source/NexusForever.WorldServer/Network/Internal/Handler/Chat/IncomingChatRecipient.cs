using System;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Chat;
using NexusForever.Shared.Game.Events;

namespace NexusForever.WorldServer.Network.Internal.Handler.Chat
{
    // One broker delivery belongs to the recipient present at admission. A later
    // login or character on the same connection cannot inherit its queued text.
    internal sealed class IncomingChatRecipient
    {
        internal IPlayer Player { get; }
        private readonly Identity identity;
        private readonly IPlayerManager players;
        private readonly IGameSession session;
        private readonly EventQueue events;
        private readonly IAccount account;
        private readonly ulong epoch;

        private IncomingChatRecipient(IPlayerManager players, Identity identity, IPlayer player)
        {
            this.players = players; this.identity = identity; Player = player;
            session = player.Session; events = session?.Events;
            account = player.Account; epoch = account?.WorldSessionEpoch ?? 0;
        }

        internal static IncomingChatRecipient Capture(IPlayerManager players, Identity identity)
        {
            IPlayer player = players.GetPlayer(identity);
            if (player == null) return null;
            var recipient = new IncomingChatRecipient(players, identity, player);
            return recipient.IsCurrent ? recipient : null;
        }

        internal bool IsCurrent => events != null && account != null
            && Player.Identity?.Id == identity.Id && Player.Identity.RealmId == identity.RealmId
            && Player.CharacterId == identity.Id && ReferenceEquals(players.GetPlayer(identity), Player)
            && ReferenceEquals(Player.Session, session) && ReferenceEquals(session.Events, events)
            && ReferenceEquals(Player.Account, account) && account.WorldSessionEpoch == epoch
            && ReferenceEquals(account.Session, session)
            && session is IPlayerSession current && ReferenceEquals(current.Player, Player)
            && session is not NetworkSession { IsDisconnecting: true };

        internal void Enqueue(Action delivery)
            => events.EnqueueThreadSafe(new PredicateEvent(() => true, () => { if (IsCurrent) delivery(); }));

        internal bool Send(ServerChat packet)
        {
            if (!IsCurrent) return false;
            session.EnqueueMessageEncrypted(packet);
            return IsCurrent;
        }
    }
}
