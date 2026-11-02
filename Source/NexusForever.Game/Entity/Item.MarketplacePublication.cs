using System.Collections.ObjectModel;
using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Entity
{
    public partial class Item
    {
        private volatile MarketplacePublication marketplacePublication;
        private IList<uint> microchipIds;
        private IList<ItemRuneSlot> runeSlots;

        private void EnsureMarketplaceMutationAllowed()
        {
            if (marketplacePublication != null)
                throw new InvalidOperationException("Item is publishing a committed marketplace result.");
        }

        internal IDisposable BeginMarketplacePublication()
        {
            lock (persistenceGate)
            {
                EnsureMarketplaceMutationAllowed();
                var publication = new MarketplacePublication(this);
                try
                {
                    foreach (var slot in RuneSlots.Distinct())
                        publication.Slots.Add(slot.ReserveMarketplacePublication());
                    return marketplacePublication = publication;
                }
                catch
                {
                    foreach (var slot in publication.Slots) slot.Dispose();
                    throw;
                }
            }
        }

        // Keep cached IList references subject to the same exclusion as scalar setters.
        private sealed class MarketplaceCollection<T>(Item owner) : Collection<T>
        {
            protected override void InsertItem(int index, T value)
            { lock (owner.persistenceGate) { owner.EnsureMarketplaceMutationAllowed(); base.InsertItem(index, value); } }
            protected override void SetItem(int index, T value)
            { lock (owner.persistenceGate) { owner.EnsureMarketplaceMutationAllowed(); base.SetItem(index, value); } }
            protected override void RemoveItem(int index)
            { lock (owner.persistenceGate) { owner.EnsureMarketplaceMutationAllowed(); base.RemoveItem(index); } }
            protected override void ClearItems()
            { lock (owner.persistenceGate) { owner.EnsureMarketplaceMutationAllowed(); base.ClearItems(); } }
        }

        private sealed class MarketplacePublication(Item owner) : IDisposable
        {
            internal List<IDisposable> Slots { get; } = [];
            private bool disposed;
            public void Dispose()
            {
                lock (owner.persistenceGate)
                {
                    if (disposed) return;
                    if (!ReferenceEquals(owner.marketplacePublication, this))
                        throw new InvalidOperationException("Item publication cannot release another owner.");
                    foreach (var slot in Slots) slot.Dispose();
                    owner.marketplacePublication = null; disposed = true;
                }
            }
        }
    }
}
