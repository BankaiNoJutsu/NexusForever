using NexusForever.Database.Friendship;
using NexusForever.Database.Friendship.Model;
using NexusForever.Server.Friendship.Game.Account;
using NexusForever.Server.Friendship.Game.Character;
using WireIdentity = NexusForever.Network.Internal.Message.Shared.Identity;

namespace NexusForever.Server.Friendship.Network.Internal.Handler.Player
{
    internal sealed record FriendshipPresenceGeneration(ulong Epoch, ulong Admission, ulong CharacterId,
        ushort RealmId, bool Closed, DateTime OccurredUtc)
    {
        internal static FriendshipPresenceGeneration Capture(uint account, WireIdentity identity,
            ulong epoch, ulong admission, DateTime? occurred, bool closed)
        {
            if (epoch == 0 && admission == 0) return null; // explicit legacy contract
            if (account == 0 || identity == null || identity.Id == 0 || identity.RealmId == 0 || epoch == 0 || admission == 0
                || occurred is not { Kind: DateTimeKind.Utc } || occurred == default(DateTime))
                throw new InvalidOperationException("Invalid versioned presence identity or occurrence time.");
            return new(epoch, admission, identity.Id, identity.RealmId, closed, occurred.Value);
        }

        internal static bool Accept(AccountModel account, FriendshipPresenceGeneration incoming)
        {
            if (account.PresenceSessionEpoch == 0)
            {
                if (account.PresenceAdmission != 0) throw new InvalidOperationException("Incomplete stored presence generation.");
                return true;
            }
            if (account.PresenceAdmission == 0 || account.PresenceCharacterId is null or 0 || account.PresenceRealmId is null or 0)
                throw new InvalidOperationException("Incomplete stored presence source identity.");
            if (incoming == null) return false; // queued legacy producers cannot overwrite a modern owner
            int order = incoming.Epoch.CompareTo(account.PresenceSessionEpoch);
            if (order == 0) order = incoming.Admission.CompareTo(account.PresenceAdmission);
            if (order != 0) return order > 0;
            if (incoming.CharacterId != account.PresenceCharacterId || incoming.RealmId != account.PresenceRealmId)
                throw new InvalidOperationException("Presence generation changed its source character identity.");
            return incoming.Closed && !account.PresenceClosed;
        }

        internal void Remember(AccountModel account)
        {
            account.PresenceSessionEpoch = Epoch; account.PresenceAdmission = Admission; account.PresenceClosed = Closed;
            account.PresenceCharacterId = CharacterId; account.PresenceRealmId = RealmId;
        }

        internal async Task CloseSupersededAsync(FriendshipContext context, Account account, CharacterManager characters)
        {
            var previous = account.ActiveCharacterIdentity;
            if (previous == null || previous.Id == CharacterId && previous.RealmId == RealmId) return;
            var old = await characters.GetCharacterAsync(previous);
            if (old == null) return;
            await FriendshipPresenceTransaction.RefreshCharacterAsync(context, old.Model, account.Id);
            await old.SetLastOnline(OccurredUtc);
        }
    }
}
