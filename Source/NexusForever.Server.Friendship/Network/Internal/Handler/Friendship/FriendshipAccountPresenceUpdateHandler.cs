using NexusForever.Database.Friendship;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Server.Friendship.Game.Account;
using Rebus.Handlers;

namespace NexusForever.Server.Friendship.Network.Internal.Handler.Friendship
{
    public class FriendshipAccountPresenceUpdateHandler : IHandleMessages<FriendshipAccountPresenceUpdateMessage>
    {
        #region Dependency Injection

        private readonly FriendshipContext _context;
        private readonly AccountManager _accountManager;

        public FriendshipAccountPresenceUpdateHandler(
            FriendshipContext context,
            AccountManager accountManager)
        {
            _context        = context;
            _accountManager = accountManager;
        }

        #endregion

        public Task Handle(FriendshipAccountPresenceUpdateMessage message)
        {
            var presence = message.Presence;
            return FriendshipAccountCommand.ExecuteAsync(_context, _accountManager, message.AccountId, message.PresenceOwner,
                async account => { if (account != null) await account.SetPresenceAsync(presence); });
        }
    }
}
