using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;

namespace NexusForever.Game.RealmBank
{
    public sealed partial class RealmBankManager
    {
        private readonly ConditionalWeakTable<IInventory, PendingSync> pendingSyncs = new();

        private void QueueSync(IPlayer player, bool open, bool publish = true)
        {
            IInventory inventory = player.Inventory;
            if (inventory == null || inventory.RequiresAuthoritativeReload || player.Session?.Events == null) return;
            PendingSync pending = pendingSyncs.GetValue(inventory, _ => new(this, player));
            if (!pending.Current())
            {
                pendingSyncs.Remove(inventory); pending = new(this, player); pendingSyncs.Add(inventory, pending);
                if (!pending.Current()) return;
            }
            pending.Open |= open;
            pending.Show |= publish;
            if (pending.Queued) return;
            pending.Queued = true;
            player.Session.Events.EnqueueEvent(new PredicateEvent(
                () => !pending.Current() || inventory.RequiresAuthoritativeReload || !inventory.IsSaveBlocked,
                pending.Publish));
        }

        private sealed class PendingSync
        {
            private readonly RealmBankManager manager;
            private readonly IPlayer player;
            private readonly IInventory inventory;
            private readonly IGameSession session;
            private readonly IAccount account;
            private readonly uint accountId;
            private readonly ulong character;
            private readonly ushort realm;
            internal bool Open, Show, Queued;
            internal PendingSync(RealmBankManager manager, IPlayer player)
            {
                this.manager = manager; this.player = player;
                inventory = player.Inventory; session = player.Session; account = player.Account;
                accountId = account.Id; character = player.CharacterId; realm = player.Identity.RealmId;
            }
            internal bool Current() => ReferenceEquals(player.Inventory, inventory) && ReferenceEquals(player.Session, session)
                && ReferenceEquals(player.Account, account) && ReferenceEquals(account.Session, session) && account.Id == accountId
                && player.CharacterId == character && player.Identity.RealmId == realm
                && session is not NetworkSession { IsDisconnecting: true }
                && player.LogoutManager?.State is not (LogoutState.Logout or LogoutState.Finished);
            internal void Publish()
            {
                Queued = false; bool load = Open, show = Show; Open = Show = false;
                if (!Current() || inventory.RequiresAuthoritativeReload) return;
                if (load) manager.EnsureLoaded(player);
                if (show && manager.HasUnlock(player)) manager.SyncRealmBankToClient(player);
            }
        }
    }
}
