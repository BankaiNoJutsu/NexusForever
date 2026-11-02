using System.Runtime.CompilerServices;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.RealmBank
{
    public sealed partial class RealmBankManager
    {
        private readonly ConditionalWeakTable<IInventory, BankLoad> loaded = new();
        private sealed class BankLoad(IAccount account, uint accountId, ushort realm, ulong character)
        {
            internal readonly object Gate = new();
            internal bool Loading, Loaded, RequiresReload;
            internal void Validate(IPlayer player)
            {
                if (!ReferenceEquals(player.Account, account) || account.Id != accountId || player.Identity.RealmId != realm || player.CharacterId != character)
                    throw new InvalidOperationException("Realm bank inventory changed its retained account/realm/Character owner.");
            }
        }
        private void LoadInventory(IPlayer player, CharacterDatabase database)
        {
            ArgumentNullException.ThrowIfNull(player);
            var inventory = player.Inventory ?? throw new InvalidOperationException("Realm bank requires its current inventory.");
            var state = loaded.GetValue(inventory, _ => new(player.Account, player.Account.Id, player.Identity.RealmId, player.CharacterId));
            lock (state.Gate)
            {
                state.Validate(player);
                if (state.RequiresReload) throw new InvalidOperationException("Partially loaded realm bank requires a fresh inventory.");
                if (state.Loading) throw new InvalidOperationException("Realm bank inventory is already loading.");
                if (state.Loaded) return;
                state.Loading = true; bool publishing = false;
                try
                {
                    // Read and construct before publishing anything into the live
                    // inventory. Failed reads/codecs can retry the same owner.
                    var items = new List<(Entity.Item Item, uint Index)>();
                    foreach (var model in database.GetRealmBankItems(player.Account.Id, player.Identity.RealmId))
                    {
                        IItem existing = inventory.GetItem(model.Id);
                        if (existing != null)
                        {
                            if (existing is Entity.Item item && item.MatchesLoadedBankRow(model, player.CharacterId)) continue;
                            state.RequiresReload = true;
                            throw new InvalidOperationException("Loaded bank identity conflicts with the retained SQL baseline.");
                        }
                        if (itemManager.GetItemInfo(model.ItemId) == null) continue; // retain the established unknown-definition policy
                        items.Add((new Entity.Item(new ItemModel
                        {
                            Id = model.Id, OwnerId = player.CharacterId, ItemId = model.ItemId,
                            Location = (ushort)InventoryLocation.RealmBank, BagIndex = model.BagIndex, StackCount = model.StackCount,
                            Charges = model.Charges, Durability = model.Durability, ExpirationTimeLeft = model.ExpirationTimeLeft,
                            Soulbound = model.Soulbound, MicrochipIds = model.MicrochipIds, RuneSlots = model.RuneSlots
                        }, itemManager), model.BagIndex));
                    }
                    state.Validate(player);
                    if (!ReferenceEquals(player.Inventory, inventory)) throw new InvalidOperationException("Realm bank lost its inventory during loading.");
                    publishing = true;
                    foreach (var item in items)
                    {
                        state.Validate(player);
                        if (!ReferenceEquals(player.Inventory, inventory)) throw new InvalidOperationException("Realm bank lost its inventory during publication.");
                        inventory.LoadItem(item.Item, InventoryLocation.RealmBank, item.Index);
                    }
                    state.Validate(player);
                    if (!ReferenceEquals(player.Inventory, inventory)) throw new InvalidOperationException("Realm bank lost its inventory during publication.");
                    state.Loaded = true;
                }
                catch
                {
                    if (publishing) state.RequiresReload = true;
                    if (state.RequiresReload && inventory is Entity.Inventory concrete) concrete.RequireRealmBankReload();
                    throw;
                }
                finally { state.Loading = false; }
            }
        }
    }
}
