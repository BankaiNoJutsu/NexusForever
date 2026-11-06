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
    public class FriendshipAccountNicknameUpdateHandler : IHandleMessages<FriendshipAccountNicknameUpdateMessage>
    {
        #region Dependency Injection

        private readonly FriendshipContext _context;
        private readonly IInternalMessagePublisher _messagePublisher;

        private readonly AccountManager _accountManager;
        private readonly ITextFilterManager _textFilterManager;
        private readonly FriendshipResultPublisher _friendshipResultPublisher;

        public FriendshipAccountNicknameUpdateHandler(
            FriendshipContext context,
            OutboxMessagePublisher messagePublisher,
            AccountManager accountManager,
            ITextFilterManager textFilterManager,
            FriendshipResultPublisher friendshipResultPublisher)
        {
            _context                   = context;
            _messagePublisher          = messagePublisher;
            _accountManager            = accountManager;
            _textFilterManager         = textFilterManager;
            _friendshipResultPublisher = friendshipResultPublisher;
        }

        #endregion

        public Task Handle(FriendshipAccountNicknameUpdateMessage message)
        {
            string nickname = message.AccountNickname;
            var identity = message.PresenceOwner?.Identity ?? message.Source;
            return FriendshipAccountCommand.ExecuteAsync(_context, _accountManager, message.AccountId, message.PresenceOwner, async account =>
            {
                Task<FriendshipResult?> task = AccountSetNicknameAsync(account, nickname);
                await _friendshipResultPublisher.PublishResultAsync(_messagePublisher, identity, task);
            });
        }

        private async Task<FriendshipResult?> AccountSetNicknameAsync(Account account, string nickname)
        {
            if (account == null)
                return FriendshipResult.PlayerNotFound;

            if (nickname != null)
            {
                if (!_textFilterManager.IsTextValid(nickname, UserText.FriendshipAccountName))
                    return FriendshipResult.InvalidDisplayName;

                if (!_textFilterManager.IsTextValid(nickname))
                    return FriendshipResult.ContainsProfanity;

                if (await _accountManager.GetAccountByNickname(nickname) != null)
                    return FriendshipResult.NameUnavailable;
            }

            await account.SetNicknameAsync(nickname);

            return null;
        }
    }
}
