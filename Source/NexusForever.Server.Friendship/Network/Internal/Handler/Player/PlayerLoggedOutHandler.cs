using NexusForever.Database.Friendship;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Server.Friendship.Game.Account;
using NexusForever.Server.Friendship.Game.Character;
using Rebus.Handlers;

namespace NexusForever.Server.Friendship.Network.Internal.Handler.Player
{
    public class PlayerLoggedOutHandler : IHandleMessages<PlayerLoggedOutMessage>
    {
        #region Dependency Injection

        private readonly FriendshipContext _context;
        private readonly CharacterManager _characterManager;

        public PlayerLoggedOutHandler(
            FriendshipContext context,
            CharacterManager characterManager)
        {
            _context          = context;
            _characterManager = characterManager;
        }

        #endregion

        public async Task Handle(PlayerLoggedOutMessage message)
        {
            var generation = FriendshipPresenceGeneration.Capture(message.AccountId, message.Identity,
                message.SessionEpoch, message.Admission, message.OccurredUtc, closed: true);
            await FriendshipPresenceTransaction.ExecuteAsync(_context, _characterManager, async () =>
            {
                if (generation != null)
                {
                    Account versionedAccount = await _characterManager.GetPresenceAccountRemoteAsync(message.AccountId);
                    if (versionedAccount == null) return false;
                    await FriendshipPresenceTransaction.LockAndRefreshAccountAsync(_context, versionedAccount.Model);
                    if (!FriendshipPresenceGeneration.Accept(versionedAccount.Model, generation)) return true;
                    Character versionedCharacter = await _characterManager.GetPresenceCharacterRemoteAsync(message.Identity.ToFriendshipIdentity(), versionedAccount.Id);
                    if (versionedCharacter != null)
                    {
                        await _context.SaveChangesAsync();
                        await FriendshipPresenceTransaction.RefreshCharacterAsync(_context, versionedCharacter.Model, versionedAccount.Id);
                    }
                    // A missing/deleted character must not discard the account
                    // tombstone, including logout-before-first-login delivery.
                    await generation.CloseSupersededAsync(_context, versionedAccount, _characterManager);
                    if (versionedCharacter != null) await versionedCharacter.SetLastOnline(generation.OccurredUtc);
                    await versionedAccount.SetLastOnline(generation.OccurredUtc);
                    await versionedAccount.SetActiveCharacterAsync(null);
                    generation.Remember(versionedAccount.Model);
                    await _context.SaveChangesAsync();
                    return true;
                }

                Character character = await _characterManager.GetCharacterAsync(message.Identity.ToFriendshipIdentity());
                if (character == null)
                    return false;

                Account account = await character.GetAccountAsync();
                if (account == null)
                    return false;

                await FriendshipPresenceTransaction.LockAndRefreshAccountAsync(_context, account.Model);
                if (!FriendshipPresenceGeneration.Accept(account.Model, null)) return true;
                await FriendshipPresenceTransaction.RefreshCharacterAsync(_context, character.Model, account.Id);
                DateTime now = DateTime.UtcNow;
                await character.SetLastOnline(now);

                // An old different-character/realm logout cannot clear the
                // current identity. Same-character sessions need a generation
                // contract to distinguish delayed broker delivery.
                var active = account.ActiveCharacterIdentity;
                if (active == null || active.Id == character.Identity.Id && active.RealmId == character.Identity.RealmId)
                {
                    await account.SetLastOnline(now);
                    await account.SetActiveCharacterAsync(null);
                }

                await _context.SaveChangesAsync();
                return true;
            });
        }
    }
}
