namespace NexusForever.Game.Entity
{
    public partial class Inventory
    {
        private volatile MarketplacePublication marketplacePublication;
        internal bool IsMarketplacePublishing => marketplacePublication != null;

        internal IDisposable BeginMarketplacePublication(Func<bool> ownsActor)
        {
            lock (MutationGate)
            {
                if (marketplacePublication != null || !ownsActor())
                    throw new InvalidOperationException("Marketplace publication lost its original inventory owner.");
                var publication = new MarketplacePublication(this);
                try
                {
                    foreach (var item in bags.Values.SelectMany(bag => bag).OfType<Item>().Distinct())
                        publication.Items.Add(item.BeginMarketplacePublication());
                    return marketplacePublication = publication;
                }
                catch
                {
                    foreach (var item in publication.Items) item.Dispose();
                    throw;
                }
            }
        }

        private sealed class MarketplacePublication(Inventory owner) : IDisposable
        {
            internal List<IDisposable> Items { get; } = [];
            private bool disposed;
            public void Dispose()
            {
                lock (owner.MutationGate)
                {
                    if (disposed) return;
                    if (!ReferenceEquals(owner.marketplacePublication, this))
                        throw new InvalidOperationException("Marketplace publication cannot release another owner.");
                    foreach (var item in Items) item.Dispose();
                    owner.marketplacePublication = null; disposed = true;
                }
            }
        }
    }
}
