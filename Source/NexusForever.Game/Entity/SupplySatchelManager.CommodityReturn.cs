using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Marketplace;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Entity
{
    public partial class TradeskillMaterial
    {
        private object commodityReturnOwner;
        private ushort? commodityStoredAmount;
        internal ushort? CommodityStoredAmount => commodityStoredAmount;
        internal void ConfirmCommoditySave(ushort? value) => commodityStoredAmount = value;
        internal void RebaseCommodityAmount(ushort? stored, ushort desired)
        {
            if (commodityReturnOwner != null) throw new InvalidOperationException("Material rebase overlaps capture.");
            commodityStoredAmount = stored; amount = desired;
            saveMask = stored.HasValue ? TradeskillMaterialSaveMask.Amount : TradeskillMaterialSaveMask.Create;
        }
        internal void ReserveCommodityReturn(object owner)
        {
            if (commodityReturnOwner != null || saveMask != TradeskillMaterialSaveMask.None)
                throw new InvalidOperationException("Commodity material requires a clean original owner.");
            commodityReturnOwner = owner;
        }
        internal void AdoptCommodityReturn(object owner, ushort value)
        {
            if (!ReferenceEquals(commodityReturnOwner, owner)) throw new InvalidOperationException("Commodity material lost its reservation.");
            amount = value; commodityStoredAmount = value; saveMask = TradeskillMaterialSaveMask.None; commodityReturnOwner = null;
        }
        internal void ReleaseCommodityReturn(object owner)
        {
            if (!ReferenceEquals(commodityReturnOwner, owner)) throw new InvalidOperationException("Commodity material cannot release another owner.");
            commodityReturnOwner = null;
        }
    }

    public partial class SupplySatchelManager
    {
        private readonly CharacterLoadOrigin loadedOrigin;
        private CommodityReturnReservation commodityReturn;
        private bool CanMutateMaterial() => player.Inventory is not Inventory inventory || !inventory.IsMarketplacePublishing;
        private void RequireCommodityMutationAllowed()
        {
            if (commodityReturn is { Settled: false } || !CanMutateMaterial()) throw new InvalidOperationException("Satchel mutation overlaps an owned commodity return.");
        }

        internal CommodityReturnReservation ReserveCommodityReturn(IItemInfo info, uint count, string database)
        {
            if (!TryGetAutoStoreCapacity(info, out uint capacity) || capacity == 0) return null;
            if (!TryGetMaterialEntry(info, out var entry))
                throw new InvalidOperationException("Commodity return lost its material definition.");
            return ReserveMaterialIncrease((ushort)entry.Id, Math.Min(count, capacity), database);
        }

        internal CommodityReturnReservation ReserveManualDeposit(IItem item, uint count, string database)
        {
            if (!TryGetMaterialEntry(item, out var entry)) return null;
            ushort id = (ushort)entry.Id;
            uint current = tradeskillMaterials.TryGetValue(id, out var existing) ? existing.Amount : 0u;
            uint accepted = Math.Min(count, maximumStackAmount - Math.Min(current, maximumStackAmount));
            return accepted == 0 ? null : ReserveMaterialIncrease(id, accepted, database);
        }

        private CommodityReturnReservation ReserveMaterialIncrease(ushort key, uint count, string database)
        {
            if (commodityReturn != null || loadedOrigin == null || loadedOrigin.Database != database
                || loadedOrigin.CharacterId != player.CharacterId || loadedOrigin.AccountId != player.Account.Id)
                throw new InvalidOperationException("Material increase lost its loaded satchel owner.");
            tradeskillMaterials.TryGetValue(key, out var existing);
            if (existing != null && existing is not TradeskillMaterial) throw new InvalidOperationException("Material increase requires its concrete material.");
            var reservation = new CommodityReturnReservation(this, key, existing as TradeskillMaterial, count);
            (existing as TradeskillMaterial)?.ReserveCommodityReturn(reservation);
            return commodityReturn = reservation;
        }

        internal sealed class CommodityReturnReservation : IDisposable
        {
            private readonly SupplySatchelManager manager;
            private readonly TradeskillMaterial original;
            internal GlobalMarketplaceManager.CommodityMaterialWrite Write { get; }
            internal bool Settled { get; private set; }
            internal bool OwnsMaterial(TradeskillMaterial material) => Settled && ReferenceEquals(manager.commodityReturn, this)
                && manager.tradeskillMaterials.TryGetValue(Write.Id, out var current) && ReferenceEquals(current, material);
            internal void SaveOrdinary(CharacterContext context)
            {
                if (!Settled || !ReferenceEquals(manager.commodityReturn, this) || context.AtomicCharacterId != manager.player.CharacterId || context.Database.CurrentTransaction == null)
                    throw new InvalidOperationException("Commodity material lost its ordinary reconciliation owner.");
                if (manager.loadedOrigin != null) CharacterDatabaseIdentity.Require(context, manager.loadedOrigin.Database);
                if (manager.tradeskillMaterials.TryGetValue(Write.Id, out var current))
                {
                    if (current is not TradeskillMaterial material) throw new InvalidOperationException("Commodity material requires its concrete owner.");
                    material.SaveCommodity(context, this);
                }
            }
            internal ushort? Current => manager.tradeskillMaterials.TryGetValue(Write.Id, out var current) ? (current as TradeskillMaterial)?.CommodityStoredAmount : null;
            internal ushort? Desired => manager.tradeskillMaterials.TryGetValue(Write.Id, out var current) ? current.Amount : null;
            private bool disposed;
            internal CommodityReturnReservation(SupplySatchelManager manager, ushort key, TradeskillMaterial original, uint count)
            {
                this.manager = manager; this.original = original;
                Write = new(key, original != null, original?.Amount ?? 0, checked((ushort)((original?.Amount ?? 0) + count)), manager.maximumStackAmount);
            }
            internal void Settle(ushort? value)
            {
                if (Settled || !ReferenceEquals(manager.commodityReturn, this) || value > Write.Cap)
                    throw new InvalidOperationException("Commodity return lost its settled satchel amount.");
                if (original != null)
                {
                    original.AdoptCommodityReturn(this, value ?? 0);
                    if (!value.HasValue) manager.tradeskillMaterials.Remove(Write.Id);
                }
                else if (value.HasValue)
                    manager.tradeskillMaterials.Add(Write.Id, new TradeskillMaterial(new CharacterTradeskillMaterialModel
                        { Id = manager.player.CharacterId, MaterialId = Write.Id, Amount = value.Value }, manager.gameTableManager, manager.CanMutateMaterial));
                Settled = true;
            }
            internal void ConfirmSave(ushort? value)
            {
                if (!Settled || !ReferenceEquals(manager.commodityReturn, this))
                    throw new InvalidOperationException("Commodity material save lost its original reservation.");
                if (manager.tradeskillMaterials.TryGetValue(Write.Id, out var current) && current is TradeskillMaterial material)
                    material.ConfirmCommoditySave(value);
            }
            internal void Rebase(ushort? stored)
            {
                if (!Settled || !ReferenceEquals(manager.commodityReturn, this) || stored > Write.Cap)
                    throw new InvalidOperationException("Material rebase lost its original owner/cap.");
                int delta = (Desired ?? 0) - (Current ?? 0);
                int desired = (stored ?? 0) + delta;
                if (desired < 0 || desired > Write.Cap) throw new InvalidOperationException("Material intent cannot fit its observed current amount.");
                if (!manager.tradeskillMaterials.TryGetValue(Write.Id, out var current))
                {
                    if (!stored.HasValue && desired == 0) return;
                    current = new TradeskillMaterial(manager.player.CharacterId, Write.Id, manager.gameTableManager, manager.CanMutateMaterial);
                    manager.tradeskillMaterials.Add(Write.Id, current);
                }
                if (current is not TradeskillMaterial concrete) throw new InvalidOperationException("Material rebase requires its concrete owner.");
                concrete.RebaseCommodityAmount(stored, checked((ushort)desired));
            }
            internal void Publish(Func<bool> allowed, Action<IWritable> send = null)
            {
                if (!allowed()) return;
                send ??= manager.player.Session.EnqueueMessageEncrypted;
                send(new ServerSupplySatchelUpdate
                    { MaterialId = Write.Id, StackCount = manager.tradeskillMaterials.TryGetValue(Write.Id, out var current) ? current.Amount : (ushort)0 });
            }
            public void Dispose()
            {
                if (disposed) return; disposed = true;
                if (!Settled) original?.ReleaseCommodityReturn(this);
                if (ReferenceEquals(manager.commodityReturn, this)) manager.commodityReturn = null;
            }
        }
    }
}
