using System.Threading.Tasks;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Network.World.Message.Model.Friendship;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Friendship
{
    public class FriendshipAccountPersonalStatusUpdatedHandler : IHandleMessages<FriendshipAccountPersonalStatusUpdatedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;

        public FriendshipAccountPersonalStatusUpdatedHandler(
            IPlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        #endregion

        public Task Handle(FriendshipAccountPersonalStatusUpdatedMessage message)
        {
            IPlayer player = playerManager.GetPlayerByAccountId(message.Account.Id);
            var session = player?.Session;
            var queue = session?.Events;
            var account = player?.Account;
            var epoch = account?.WorldSessionEpoch;
            uint accountId = message.Account.Id;
            if (queue == null || account == null || account.Id != accountId) return Task.CompletedTask;
            bool Current() => ReferenceEquals(playerManager.GetPlayerByAccountId(accountId), player)
                && ReferenceEquals(player.Session, session) && ReferenceEquals(session.Events, queue)
                && ReferenceEquals(player.Account, account) && account.WorldSessionEpoch == epoch
                && ReferenceEquals(account.Session, session)
                && session is IPlayerSession bound && ReferenceEquals(bound.Player, player)
                && session is not NetworkSession { IsDisconnecting: true };
            string status = message.Account.Status ?? string.Empty, nickname = message.Account.Nickname ?? string.Empty;
            var presence = message.Account.Presence;
            bool blocked = message.Account.BlockAccountFriendRequests;
            if (Current()) queue.EnqueueThreadSafe(new PredicateEvent(() => true, () =>
            {
                if (Current()) session.EnqueueMessageEncrypted(new ServerFriendshipAccountPersonalStatus
                {
                    AccountPublicStatus = status, AccountNickname = nickname,
                    Presence = presence, BlockStrangerRequests = blocked
                });
            }));

            return Task.CompletedTask;
        }
    }
}
