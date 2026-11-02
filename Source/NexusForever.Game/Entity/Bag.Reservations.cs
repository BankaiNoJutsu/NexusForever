using NexusForever.Game.Abstract.Entity;

namespace NexusForever.Game.Entity
{
    public partial class Bag
    {
        private readonly object mutationGate = new();
        private readonly Dictionary<uint, SlotReservation> reservations = [];

        /// <summary>Available empty slots; SlotsRemaining still counts physical empty slots.</summary>
        public uint AvailableSlots
        {
            get
            {
                lock (mutationGate)
                    return SlotsRemaining - (uint)reservations.Keys.Count(index => items[index] == null);
            }
        }

        public bool HasReservedSlots
        {
            get
            {
                lock (mutationGate)
                    return reservations.Count != 0 || items.OfType<Item>().Any(item => item.HasRewardMutationReservation);
            }
        }

        public bool IsSlotReserved(uint bagIndex)
        {
            lock (mutationGate)
                return reservations.ContainsKey(bagIndex) || (bagIndex < items.Length
                    && items[bagIndex] is Item item && item.HasRewardMutationReservation);
        }

        private void ValidateIndex(uint index)
        {
            if (index >= items.Length) throw new ArgumentOutOfRangeException(nameof(index));
        }

        private void ValidateResident(IItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.Location != Location || item.BagIndex >= items.Length || !ReferenceEquals(items[item.BagIndex], item))
                throw new ArgumentException("Item is not the current occupant of this bag slot.");
        }

        private void EnsureSlotAvailable(uint index)
        {
            if (reservations.ContainsKey(index)) throw new InvalidOperationException("Bag slot is reserved for a pending delivery.");
            if (items[index] is Item item && item.HasRewardMutationReservation)
                throw new InvalidOperationException("Bag occupant is reserved for a pending delivery.");
        }

        private void EnsureNoReservations()
        {
            if (reservations.Count != 0 || items.OfType<Item>().Any(item => item.HasRewardMutationReservation))
                throw new InvalidOperationException("Bag has a pending delivery reservation.");
        }

        internal bool TryReserveRewardMutation(Item item, out Item.RewardMutationReservation reservation)
        {
            lock (mutationGate)
            {
                reservation = null;
                if (item == null || item.Location != Location || item.BagIndex >= items.Length
                    || !ReferenceEquals(items[item.BagIndex], item))
                    return false;
                return item.TryReserveRewardMutationFromBag(out reservation);
            }
        }

        internal bool TryReserveRewardPlan(IReadOnlyDictionary<uint, IItem> expected,
            out SlotReservation slots, out Dictionary<Item, Item.RewardMutationReservation> itemReservations)
        {
            lock (mutationGate)
            {
                itemReservations = [];
                if (!TryReserveSlots(expected, out slots)) return false;
                foreach (IItem candidate in expected.Values.Where(item => item != null))
                {
                    if (candidate is not Item item || itemReservations.ContainsKey(item)
                        || !TryReserveRewardMutation(item, out Item.RewardMutationReservation token))
                    {
                        foreach (var held in itemReservations.Values) held.Dispose();
                        itemReservations.Clear();
                        slots.Dispose();
                        slots = null;
                        return false;
                    }
                    itemReservations.Add(item, token);
                }
                return true;
            }
        }

        /// <summary>
        /// Reserve exact current occupants without changing items, allocating IDs,
        /// or retaining a monitor across database work. Inventory must also reserve
        /// occupied Items against direct scalar mutations before enabling delivery.
        /// </summary>
        internal bool TryReserveSlots(IReadOnlyDictionary<uint, IItem> expected, out SlotReservation reservation)
        {
            ArgumentNullException.ThrowIfNull(expected);
            Dictionary<uint, IItem> captured = expected.ToDictionary(pair => pair.Key, pair => pair.Value);
            lock (mutationGate)
            {
                reservation = null;
                if (captured.Count == 0 || captured.Any(pair => pair.Key >= items.Length
                    || reservations.ContainsKey(pair.Key) || !ReferenceEquals(items[pair.Key], pair.Value)
                    || (pair.Value != null && (pair.Value.Location != Location || pair.Value.BagIndex != pair.Key))))
                    return false;
                reservation = new SlotReservation(this, captured);
                foreach (uint index in captured.Keys) reservations.Add(index, reservation);
                return true;
            }
        }

        internal sealed class SlotReservation : IDisposable
        {
            private readonly Bag bag;
            private readonly Dictionary<uint, IItem> expected;
            private bool released;

            internal SlotReservation(Bag bag, Dictionary<uint, IItem> expected)
            {
                this.bag = bag;
                this.expected = expected;
            }

            internal bool IsCurrent
            {
                get
                {
                    lock (bag.mutationGate)
                        return !released && expected.All(pair => bag.reservations.TryGetValue(pair.Key, out var owner)
                            && ReferenceEquals(owner, this) && ReferenceEquals(bag.items[pair.Key], pair.Value));
                }
            }

            /// <summary>
            /// Admit an already committed Item after its persistence owner adopted
            /// location/index. This changes only the bag array and physical count.
            /// The capability does not itself establish database commit evidence.
            /// </summary>
            internal void PublishItem(IItem item, uint index)
            {
                ArgumentNullException.ThrowIfNull(item);
                lock (bag.mutationGate)
                {
                    if (released || !expected.TryGetValue(index, out IItem old) || old != null
                        || !bag.reservations.TryGetValue(index, out SlotReservation owner) || !ReferenceEquals(owner, this))
                        throw new InvalidOperationException("Slot is not an empty destination owned by this reservation.");
                    if (bag.items[index] != null || item.Location != bag.Location || item.BagIndex != index)
                        throw new InvalidOperationException("Published item does not match its committed destination.");
                    bag.items[index] = item;
                    expected[index] = item;
                    --bag.slotsRemaining;
                }
            }

            // A committed commodity sale can free and reuse its exact captured
            // slot. Only this reservation may replace that original occupant.
            internal void PublishCommodityReplacement(IItem item, uint index)
            {
                lock (bag.mutationGate)
                {
                    if (released || !expected.TryGetValue(index, out IItem old)
                        || !bag.reservations.TryGetValue(index, out var owner) || !ReferenceEquals(owner, this)
                        || !ReferenceEquals(bag.items[index], old)
                        || item != null && (item.Location != bag.Location || item.BagIndex != index))
                        throw new InvalidOperationException("Commodity replacement lost its exact captured slot.");
                    if (old != null) ++bag.slotsRemaining;
                    if (item != null) --bag.slotsRemaining;
                    bag.items[index] = item; expected[index] = item;
                }
            }

            public void Dispose()
            {
                lock (bag.mutationGate)
                {
                    if (released) return;
                    foreach (uint index in expected.Keys) bag.reservations.Remove(index);
                    released = true;
                }
            }
        }
    }
}
