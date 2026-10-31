using System.Collections;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NLog;

namespace NexusForever.Game.Entity
{
    public partial class Bag : IBag
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        public InventoryLocation Location { get; }
        public uint Slots { get { lock (mutationGate) return (uint)items.Length; } }
        public uint SlotsRemaining { get { lock (mutationGate) return slotsRemaining; } private set => slotsRemaining = value; }
        private uint slotsRemaining;

        private IItem[] items;
        private readonly IItemManager itemManager;

        public Bag(
            InventoryLocation location,
            uint capacity,
            IItemManager itemManager = null)
        {
            Location       = location;
            SlotsRemaining = capacity;
            items          = new IItem[capacity];
            this.itemManager = itemManager;

            log.Trace($"Initialised new bag {Location} with {capacity} slots.");
        }

        public void Save(CharacterContext context)
        {
            IItem[] snapshot = CreateSnapshot();
            for (uint index = 0; index < snapshot.Length; ++index)
            {
                IItem item = snapshot[index];
                if (item == null) continue;
                lock (mutationGate)
                {
                    if (index < items.Length && ReferenceEquals(items[index], item) && index != item.BagIndex)
                    {
                        EnsureSlotAvailable(index);
                        log.Warn($"Item with guid: 0x{item.Guid:X16} has incorrect slot: {item.BagIndex}, setting to slot: {index}");
                        item.BagIndex = index;
                    }
                }
                // Item enrollment can query SQL. Do not retain the bag gate here.
                item.Save(context);
            }
        }

        /// <summary>
        /// Returns <see cref="IItem"/> with the supplied guid.
        /// </summary>
        public IItem GetItem(ulong guid)
        {
            lock (mutationGate)
            {
                return items.SingleOrDefault(i => i != null && i.Guid == guid);
            }
        }

        /// <summary>
        /// Returns <see cref="IItem"/> found at the supplied bag index.
        /// </summary>
        public IItem GetItem(uint bagIndex)
        {
            lock (mutationGate)
            {
                if (bagIndex >= items.Length)
                    throw new ArgumentOutOfRangeException();

                return items[(int)bagIndex];
            }
        }

        /// <summary>
        /// Returns the first empty bag index.
        /// </summary>
        public uint? GetFirstAvailableBagIndex()
        {
            lock (mutationGate)
            {
                for (uint bagIndex = 0u; bagIndex < items.Length; bagIndex++)
                    if (items[bagIndex] == null && !reservations.ContainsKey(bagIndex))
                        return bagIndex;

                return null;
            }
        }

        /// <summary>
        /// Return the first empty bag index for <see cref="ItemSlot"/>.
        /// </summary>
        public uint? GetFirstAvailableBagIndex(ItemSlot slot)
        {
            lock (mutationGate)
            {
                if (Location != InventoryLocation.Equipped)
                    return null;

                // find first free bag index, some items can be equipped into multiple slots
                foreach (uint bagIndex in itemManager?.GetEquippedBagIndexes(slot) ?? Enumerable.Empty<EquippedItem>())
                    if (items[bagIndex] == null && !reservations.ContainsKey(bagIndex))
                        return bagIndex;

                return null;
            }
        }

        /// <summary>
        /// Add <see cref="IItem"/> to the bag at the supplied bag index.
        /// </summary>
        public void AddItem(IItem item, uint bagIndex)
        {
            lock (mutationGate)
            {
                ValidateIndex(bagIndex);
                EnsureSlotAvailable(bagIndex);
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                if (item.Location != InventoryLocation.None)
                    throw new ArgumentException();
                if (items[bagIndex] != null)
                    throw new ArgumentException();

                item.Location = Location;
                item.BagIndex = bagIndex;
                items[item.BagIndex] = item;

                checked
                {
                    SlotsRemaining--;
                }

                log.Trace($"Added item 0x{item.Guid:X16} to bag {Location} at index {item.BagIndex}.");
            }
        }

        /// <summary>
        /// Remove existing <see cref="IItem"/> from the bag at the supplied bag index.
        /// </summary>
        public void RemoveItem(IItem item)
        {
            lock (mutationGate)
            {
                ValidateResident(item);
                EnsureSlotAvailable(item.BagIndex);
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                if (item.Location != Location)
                    throw new ArgumentException();
                if (items[item.BagIndex] == null)
                    throw new ArgumentException();

                items[item.BagIndex] = null;

                log.Trace($"Removed item 0x{item.Guid:X16} from bag {Location} at index {item.BagIndex}.");

                item.PreviousLocation = item.Location;
                item.PreviousBagIndex = item.BagIndex;
                item.Location = InventoryLocation.None;
                item.BagIndex = 0u;

                checked
                {
                    SlotsRemaining++;
                }

                if (SlotsRemaining > Slots)
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Move existing <see cref="IItem"/> in <see cref="IBag"/> to supplied empty bag index.
        /// </summary>
        public void MoveItem(IItem item, uint bagIndex)
        {
            lock (mutationGate)
            {
                ValidateResident(item);
                ValidateIndex(bagIndex);
                EnsureSlotAvailable(item.BagIndex);
                EnsureSlotAvailable(bagIndex);
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                if (item.Location != Location)
                    throw new ArgumentException();
                if (items[item.BagIndex] == null)
                    throw new ArgumentException();
                if (items[bagIndex] != null)
                    throw new ArgumentException();

                item.PreviousLocation = item.Location;
                item.PreviousBagIndex = item.BagIndex;

                items[item.BagIndex] = null;

                item.BagIndex = bagIndex;
                items[bagIndex] = item;

                log.Trace($"Moved item 0x{item.Guid:X16} from bag {Location} at index {item.PreviousBagIndex} to index {item.BagIndex}.");
            }
        }

        /// <summary>
        /// Swap 2 existing <see cref="IItem"/> bag indexes in <see cref="IBag"/> with each other.
        /// </summary>
        public void SwapItem(IItem item, IItem item2)
        {
            lock (mutationGate)
            {
                ValidateResident(item);
                ValidateResident(item2);
                EnsureSlotAvailable(item.BagIndex);
                EnsureSlotAvailable(item2.BagIndex);
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                if (item2 == null)
                    throw new ArgumentNullException(nameof(item2));
                if (item.Location != Location)
                    throw new ArgumentException();
                if (item2.Location != Location)
                    throw new ArgumentException();
                if (items[item.BagIndex] == null)
                    throw new ArgumentException();
                if (items[item2.BagIndex] == null)
                    throw new ArgumentException();

                item.PreviousLocation = item.Location;
                item2.PreviousLocation = item2.Location;
                item.PreviousBagIndex = item.BagIndex;
                item2.PreviousBagIndex = item2.BagIndex;

                items[item.BagIndex] = item2;
                item2.BagIndex = item.BagIndex;

                items[item2.PreviousBagIndex] = item;
                item.BagIndex = item2.PreviousBagIndex;

                log.Trace($"Swapped items 0x{item.Guid:X16} and 0x{item2.Guid:X16} from bag {Location} between index {item.BagIndex} and {item2.BagIndex}.");
            }
        }

        /// <summary>
        /// Resize bag with supplied capacity change.
        /// </summary>
        public void Resize(int capacityChange)
        {
            lock (mutationGate)
            {
                EnsureNoReservations();
                long newSize = (long)items.Length + capacityChange;
                if (newSize < 0 || newSize > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(capacityChange));
                if (newSize < items.Length && items.Skip((int)newSize).Any(item => item != null))
                    throw new InvalidOperationException("Cannot remove occupied bag slots.");
                uint remaining = checked((uint)((long)SlotsRemaining + capacityChange));
                Array.Resize(ref items, (int)newSize);
                SlotsRemaining = remaining;
                log.Trace($"Resized bag {Location} to {items.Length} slots.");
            }
        }

        public IItem[] CreateSnapshot()
        {
            lock (mutationGate)
            {
                return items.ToArray();
            }
        }

        public void RestoreSnapshot(IItem[] snapshot)
        {
            lock (mutationGate)
            {
                EnsureNoReservations();
                ArgumentNullException.ThrowIfNull(snapshot);
                if (snapshot.OfType<Item>().Any(item => item.HasRewardMutationReservation))
                    throw new InvalidOperationException("Snapshot contains an Item reserved for a pending delivery.");

                items = snapshot.ToArray();
                uint slotsRemaining = (uint)items.Length;
                for (uint bagIndex = 0u; bagIndex < items.Length; bagIndex++)
                {
                    IItem item = items[bagIndex];
                    if (item == null)
                        continue;

                    item.Location = Location;
                    item.BagIndex = bagIndex;
                    slotsRemaining--;
                }

                SlotsRemaining = slotsRemaining;
            }
        }

        public IEnumerator<IItem> GetEnumerator()
        {
            lock (mutationGate)
            {
                return items.Where(i => i != null).ToArray().AsEnumerable().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
