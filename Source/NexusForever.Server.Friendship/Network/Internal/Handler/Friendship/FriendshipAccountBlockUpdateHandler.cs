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

        public async Task Handle(FriendshipAccountBlockUpdateMessage message)
        {
            Task<FriendshipResult?> task = UpdateBlockStateAsync(message);
            await _friendshipResultPublisher.PublishResultAsync(_messagePublisher, message.Identity, task);

            await _context.SaveChangesAsync();
        }

        private async Task<FriendshipResult?> UpdateBlockStateAsync(FriendshipAccountBlockUpdateMessage message)
        {
            Account account = await _accountManager.GetAccountAsync(message.AccountId);
            if (account == null)
                return FriendshipResult.FriendshipNotFound;

            await account.SetBlockAccountFriendRequestsAsync(message.BlockFriendRequests);

            return null;
        }
    }
}
