using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Entity
{
    public partial class Item
    {
        internal bool MatchesLoadedBankRow(RealmBankItemModel row, ulong character)
        {
            lock (persistenceGate)
            {
                if (Location != InventoryLocation.RealmBank || CharacterId != character || activeSave != null
                    || uncertainStates != null || uncertainBankStates != null) return false;
                return SameBankState(realmBankBaseline, new ItemModel
                {
                    Id = row.Id, ItemId = row.ItemId, BagIndex = row.BagIndex, StackCount = row.StackCount,
                    Charges = row.Charges, Durability = row.Durability, ExpirationTimeLeft = row.ExpirationTimeLeft,
                    Soulbound = row.Soulbound, MicrochipIds = row.MicrochipIds, RuneSlots = row.RuneSlots
                });
            }
        }

        internal void RestoreBankPreparationMetadata(bool bound, uint[] microchips, ItemRuneSlot[] runes)
        {
            lock (persistenceGate)
            {
                EnsureRewardMutationAllowed();
                soulbound = bound; MarkDirty(ItemSaveMask.Soulbound);
                MicrochipIds.Clear(); foreach (uint chip in microchips) MicrochipIds.Add(chip);
                RuneSlots.Clear(); foreach (var slot in runes) RuneSlots.Add(new ItemRuneSlot(slot.Type, slot.RuneItem2Id));
                MarkDirty(ItemSaveMask.MicrochipIds); MarkDirty(ItemSaveMask.RuneSlots);
            }
        }
        // One immutable actor capture reused by fresh-context provider attempts.
        // SQL settlement consumes only these revisions, never later live intent.
        internal sealed class BankSaveSnapshot
        {
            private readonly Item owner;
            private readonly ItemModel model;
            internal readonly ItemSaveMask Mask;
            internal readonly IReadOnlyDictionary<ItemSaveMask, long> Revisions;
            internal readonly bool Deleted;
            internal ItemModel Model => CopyModel(model);

            internal BankSaveSnapshot(Item owner)
            {
                this.owner = owner;
                lock (owner.persistenceGate)
                {
                    if (owner.activeSave != null || owner.rewardMutationReservation != null || owner.requiresAuthoritativeRewardReload)
                        throw new InvalidOperationException("Bank capture requires its unreserved Item owner.");
                    model = owner.CaptureModel(); Mask = owner.saveMask;
                    Revisions = new System.Collections.ObjectModel.ReadOnlyDictionary<ItemSaveMask, long>(new Dictionary<ItemSaveMask, long>(owner.fieldRevisions));
                    Deleted = owner.deleteRequested;
                }
            }

            internal ItemModel Stage(CharacterContext context, bool bank, bool consumed)
            {
                lock (owner.persistenceGate)
                {
                    if (bank && model.Location != (ushort)InventoryLocation.RealmBank
                        || consumed && (model.Location != (ushort)InventoryLocation.None || !Deleted))
                        throw new InvalidOperationException("Captured bank route is inconsistent.");
                    owner.SavePersistence(context, bank, frozenBank: this);
                    return consumed ? Model : CopyModel(bank ? owner.activeSave.BankAfter : owner.activeSave?.LastState);
                }
            }
            internal void StageNormal(CharacterContext context)
            {
                lock (owner.persistenceGate)
                    owner.SavePersistence(context, false, frozenBank: this, frozenNormal: true);
            }
        }
    }
}
