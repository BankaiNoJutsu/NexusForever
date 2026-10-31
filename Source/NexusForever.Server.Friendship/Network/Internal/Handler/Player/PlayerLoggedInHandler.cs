using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NexusForever.Database.Friendship;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Server.Friendship.Game.Account;
using NexusForever.Server.Friendship.Game.Character;
using Rebus.Handlers;

namespace NexusForever.Server.Friendship.Network.Internal.Handler.Player
{
    public class PlayerLoggedInHandler : IHandleMessages<PlayerLoggedInMessage>
    {
        #region Dependency Injection

        private readonly FriendshipContext _context;
        private readonly IInternalMessagePublisher _messagePublisher;

        private readonly CharacterManager _characterManager;
        private readonly AccountManager _accountManager;

        public PlayerLoggedInHandler(
            FriendshipContext context,
            OutboxMessagePublisher outboxMessagePublisher,
            CharacterManager characterManager,
            AccountManager accountManager)
        {
            _context          = context;
            _messagePublisher = outboxMessagePublisher;
            _characterManager = characterManager;
            _accountManager   = accountManager;
        }

        #endregion

        public async Task Handle(PlayerLoggedInMessage message)
        {
            var generation = FriendshipPresenceGeneration.Capture(message.AccountId, message.Identity,
                message.SessionEpoch, message.Admission, message.OccurredUtc, closed: false);
            await FriendshipPresenceTransaction.ExecuteAsync(_context, _characterManager, async () =>
            {
                Account account = await _accountManager.GetAccountRemoteAsync(message.AccountId);
                if (account == null)
                    return false;

                await FriendshipPresenceTransaction.LockAndRefreshAccountAsync(_context, account.Model);
                if (!FriendshipPresenceGeneration.Accept(account.Model, generation)) return true;

                await _context.SaveChangesAsync();

                Character character = await _characterManager.GetPresenceCharacterRemoteAsync(message.Identity.ToFriendshipIdentity(), account.Id);
                if (character == null)
                    return false;

                await _context.SaveChangesAsync();
                await FriendshipPresenceTransaction.RefreshCharacterAsync(_context, character.Model, account.Id);

                if (generation != null) await generation.CloseSupersededAsync(_context, account, _characterManager);

                await account.SetLastOnline(null);
                await account.SetActiveCharacterAsync(message.Identity.ToFriendshipIdentity());

                await character.SetLastOnline(null);
                generation?.Remember(account.Model);

                await SendAccountMessages(account);
                await SendCharacterMessages(character);

                await _context.SaveChangesAsync();
                return true;
            });
        }

        private async Task SendAccountMessages(Account account)
        {
            await account.SendFriendsAsync();
            await account.SendFriendInvitesAsync();

            await _messagePublisher.PublishAsync(new FriendshipAccountPersonalStatusUpdatedMessage
            {
                Account = await account.ToInternalAccountAsync(),
            });
        }

        private async Task SendCharacterMessages(Character character)
        {
            await character.SendFriendsAsync();
            await character.SendFriendInvitesAsync();
        }
    }
}
