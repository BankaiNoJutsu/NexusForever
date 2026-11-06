using NexusForever.Database.Friendship;
using NexusForever.Game.Static.Friendship;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Server.Friendship.Game.Account;
using NexusForever.Server.Friendship.Game.Friend;
using Rebus.Handlers;

namespace NexusForever.Server.Friendship.Network.Internal.Handler.Friendship
{
    public class FriendshipAccountBlockUpdateHandler : IHandleMessages<FriendshipAccountBlockUpdateMessage>
    {
        #region Dependency Injection

        private readonly FriendshipContext _context;
        private readonly IInternalMessagePublisher _messagePublisher;

        private readonly AccountManager _accountManager;
        private readonly FriendshipResultPublisher _friendshipResultPublisher;

        public FriendshipAccountBlockUpdateHandler(
            FriendshipContext context,
            OutboxMessagePublisher messagePublisher,
            AccountManager accountManager,
            FriendshipResultPublisher friendshipResultPublisher)
        {
            _context                   = context;
            _messagePublisher          = messagePublisher;
            _accountManager            = accountManager;
            _friendshipResultPublisher = friendshipResultPublisher;
        }

        #endregion

        public Task Handle(FriendshipAccountBlockUpdateMessage message)
        {
            bool blocked = message.BlockFriendRequests;
            var identity = message.PresenceOwner?.Identity ?? message.Identity;
            return FriendshipAccountCommand.ExecuteAsync(_context, _accountManager, message.AccountId, message.PresenceOwner, async account =>
            {
                Task<FriendshipResult?> task = UpdateBlockStateAsync(account, blocked);
                await _friendshipResultPublisher.PublishResultAsync(_messagePublisher, identity, task);
            });
        }

        private async Task<FriendshipResult?> UpdateBlockStateAsync(Account account, bool blocked)
        {
            if (account == null)
                return FriendshipResult.FriendshipNotFound;

            await account.SetBlockAccountFriendRequestsAsync(blocked);

            return null;
        }
    }
}
