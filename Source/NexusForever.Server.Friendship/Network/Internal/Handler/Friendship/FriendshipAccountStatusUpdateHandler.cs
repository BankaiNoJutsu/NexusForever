using NexusForever.Database.Friendship;
using NexusForever.Game.Static.Friendship;
using NexusForever.GameTable.Text.Filter;
using NexusForever.GameTable.Text.Static;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Server.Friendship.Game.Account;
using NexusForever.Server.Friendship.Game.Friend;
using Rebus.Handlers;

namespace NexusForever.Server.Friendship.Network.Internal.Handler.Friendship
{
    public class FriendshipAccountStatusUpdateHandler : IHandleMessages<FriendshipAccountStatusUpdateMessage>
    {
        #region Dependency Injection

        private readonly FriendshipContext _context;
        private readonly IInternalMessagePublisher _messagePublisher;

        private readonly AccountManager _accountManager;
        private readonly ITextFilterManager _textFilterManager;
        private readonly FriendshipResultPublisher _friendshipResultPublisher;

        public FriendshipAccountStatusUpdateHandler(
            FriendshipContext context,
            OutboxMessagePublisher messagePublisher,
            AccountManager accountManager,
            ITextFilterManager textFilterManager,
            FriendshipResultPublisher friendshipResultPublisher)
        {
            _context                   = context;
            _accountManager            = accountManager;
            _textFilterManager         = textFilterManager;
            _messagePublisher          = messagePublisher;
            _friendshipResultPublisher = friendshipResultPublisher;
        }

        #endregion

        public Task Handle(FriendshipAccountStatusUpdateMessage message)
        {
            string status = message.Status;
            var identity = message.PresenceOwner?.Identity ?? message.Identity;
            return FriendshipAccountCommand.ExecuteAsync(_context, _accountManager, message.AccountId, message.PresenceOwner, async account =>
            {
                Task<FriendshipResult?> task = SetAccountStatus(account, status);
                await _friendshipResultPublisher.PublishResultAsync(_messagePublisher, identity, task);
            });
        }

        private async Task<FriendshipResult?> SetAccountStatus(Account account, string status)
        {
            if (account == null)
                return FriendshipResult.FriendshipNotFound;

            if (status != null)
            {
                if (!_textFilterManager.IsTextValid(status))
                    return FriendshipResult.ContainsProfanity;

                if (!_textFilterManager.IsTextValid(status, UserText.FriendshipAccountPublicNote))
                    return FriendshipResult.InvalidPublicNote;
            }

            await account.SetStatusAsync(status);

            return null;
        }
    }
}
