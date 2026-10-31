using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Entity
{
    /// <summary>Acquire before spending/removal; logical leases hold no monitor across work.</summary>
    public static class InventoryMutationScope
    {
        public static bool TryAcquire(IInventory inventory, out IDisposable scope) => TryAcquire([inventory], out scope);

        public static bool TryAcquire(IEnumerable<IInventory> inventories, out IDisposable scope)
        {
            ArgumentNullException.ThrowIfNull(inventories);
            var leases = new List<IDisposable>();
            scope = null;
            try
            {
                var seen = new HashSet<IInventory>(ReferenceEqualityComparer.Instance);
                foreach (var inventory in inventories)
                {
                    if (inventory == null || !seen.Add(inventory) || !inventory.SupportsTransactionalSoulboundDelivery) continue;
                    if (!inventory.TryAcquireMutationLease(out var held))
                    {
                        foreach (var lease in leases.AsEnumerable().Reverse()) lease.Dispose();
                        return false;
                    }
                    if (held != null) leases.Add(held);
                }
                scope = new Leases(leases);
                return true;
            }
            catch
            {
                foreach (var lease in leases.AsEnumerable().Reverse()) lease.Dispose();
                throw;
            }
        }

        private sealed class Leases(List<IDisposable> leases) : IDisposable
        {
            private int released;
            public void Dispose()
            {
                if (Interlocked.Exchange(ref released, 1) != 0) return;
                foreach (var lease in leases.AsEnumerable().Reverse()) lease.Dispose();
            }
        }
    }
}
