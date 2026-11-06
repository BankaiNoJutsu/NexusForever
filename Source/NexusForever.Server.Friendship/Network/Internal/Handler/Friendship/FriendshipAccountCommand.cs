using NexusForever.Database.Friendship;
using NexusForever.Network.Internal.Message.Friendship.Shared;
using NexusForever.Server.Friendship.Game.Account;
using NexusForever.Server.Friendship.Network.Internal.Handler.Player;
using Account = NexusForever.Server.Friendship.Game.Account.Account;

namespace NexusForever.Server.Friendship.Network.Internal.Handler.Friendship
{
    internal static class FriendshipAccountCommand
    {
        internal static Task ExecuteAsync(FriendshipContext context, AccountManager accounts, uint accountId,
            FriendshipAccountCommandOwner source, Func<Account, Task> update)
        {
            ulong epoch = source?.SessionEpoch ?? 0, admission = source?.Admission ?? 0;
            ulong character = source?.Identity?.Id ?? 0;
            ushort realm = source?.Identity?.RealmId ?? 0;
            if (source != null && (accountId == 0 || epoch == 0 || admission == 0 || character == 0 || realm == 0))
                throw new InvalidOperationException("Incomplete account command presence owner.");

            return FriendshipPresenceTransaction.ExecuteAsync(context, accounts, async () =>
            {
                Account account = await accounts.GetAccountAsync(accountId);
                if (account == null)
                {
                    if (source != null) throw new InvalidOperationException("Account command awaits its presence admission.");
                    await update(null);
                }
                else
                {
                    await FriendshipPresenceTransaction.LockAndRefreshAccountAsync(context, account.Model);
                    var model = account.Model;
                    if (source == null)
                    {
                        // Old queued commands cannot overwrite a versioned presence owner.
                        if (model.PresenceSessionEpoch != 0 || model.PresenceAdmission != 0) return true;
                    }
                    else
                    {
                        if (model.PresenceSessionEpoch < epoch || model.PresenceSessionEpoch == epoch && model.PresenceAdmission < admission)
                            throw new InvalidOperationException("Account command awaits its presence admission.");
                        if (model.PresenceSessionEpoch != epoch || model.PresenceAdmission != admission || model.PresenceClosed
                            || model.PresenceCharacterId != character || model.PresenceRealmId != realm) return true;
                    }
                    await update(account);
                }
                await context.SaveChangesAsync();
                return true;
            });
        }
    }
}
