using NexusForever.Game.Static.Crafting;

namespace NexusForever.Game.Abstract.Entity
{
    public sealed class ItemRuneSlot
    {
        private readonly object mutationGate = new();
        private int publicationOwners;
        private RuneType type;
        private uint runeItem2Id;
        public RuneType Type
        {
            get { lock (mutationGate) return type; }
            set { lock (mutationGate) { EnsureWritable(); type = value; } }
        }
        public uint RuneItem2Id
        {
            get { lock (mutationGate) return runeItem2Id; }
            set { lock (mutationGate) { EnsureWritable(); runeItem2Id = value; } }
        }

        private void EnsureWritable()
        {
            if (publicationOwners != 0)
                throw new InvalidOperationException("Rune slot is publishing a committed marketplace result.");
        }

        internal IDisposable ReserveMarketplacePublication()
        {
            lock (mutationGate) { publicationOwners++; return new Publication(this); }
        }

        private sealed class Publication(ItemRuneSlot slot) : IDisposable
        {
            private bool disposed;
            public void Dispose()
            {
                lock (slot.mutationGate)
                {
                    if (disposed) return;
                    slot.publicationOwners--; disposed = true;
                }
            }
        }

        public ItemRuneSlot()
        {
        }

        public ItemRuneSlot(RuneType type, uint runeItem2Id = 0u)
        {
            Type        = type;
            RuneItem2Id = runeItem2Id;
        }
    }
}
