using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Item;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NetworkItem = NexusForever.Network.World.Message.Model.Shared.Item;

namespace NexusForever.Game.Entity
{
    public class Item : IItem
    {
        /// <summary>
        /// Determines which fields need saving for <see cref="IItem"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum ItemSaveMask
        {
            None               = 0x0000,
            Create             = 0x0001,
            Delete             = 0x0002,
            CharacterId        = 0x0004,
            Location           = 0x0008,
            BagIndex           = 0x0010,
            StackCount         = 0x0020,
            Charges            = 0x0040,
            Durability         = 0x0080,
            ExpirationTimeLeft = 0x0100,
            Soulbound          = 0x0200,
            MicrochipIds       = 0x0400,
            RuneSlots          = 0x0800
        }

        public uint Id => Info?.Id ?? SpellEntry.Id;
        public IItemInfo Info { get; }
        public Spell4BaseEntry SpellEntry { get; }
        public ulong Guid { get; }

        public ulong? CharacterId
        {
            get => characterId;
            set
            {
                characterId = value;
                saveMask |= ItemSaveMask.CharacterId;
            }
        }

        private ulong? characterId;

        public InventoryLocation Location
        {
            get => location;
            set
            {
                location = value;
                saveMask |= ItemSaveMask.Location;
            }
        }

        private InventoryLocation location;

        public InventoryLocation PreviousLocation { get; set; }

        public uint BagIndex
        {
            get => bagIndex;
            set
            {
                bagIndex = value;
                saveMask |= ItemSaveMask.BagIndex;
            }
        }

        private uint bagIndex;

        public uint PreviousBagIndex { get; set; }

        public uint StackCount
        {
            get => stackCount;
            set
            {
                if (value > Info.Entry.MaxStackCount)
                    throw new ArgumentOutOfRangeException();

                stackCount = value;
                saveMask |= ItemSaveMask.StackCount;
            }
        }

        private uint stackCount;

        public uint Charges
        {
            get => charges;
            set
            {
                if (value > Info.Entry.MaxCharges)
                    throw new ArgumentOutOfRangeException();

                charges = value;
                saveMask |= ItemSaveMask.Charges;
            }
        }

        private uint charges;

        public float Durability
        {
            get => durability;
            set
            {
                if (value > 1.0f)
                    throw new ArgumentOutOfRangeException();

                durability = value;
                saveMask |= ItemSaveMask.Durability;
            }
        }

        private float durability;

        public IList<uint> MicrochipIds { get; } = new List<uint>();

        public IList<ItemRuneSlot> RuneSlots { get; } = new List<ItemRuneSlot>();

        public uint ExpirationTimeLeft
        {
            get => expirationTimeLeft;
            set
            {
                expirationTimeLeft = value;
                saveMask |= ItemSaveMask.ExpirationTimeLeft;
            }
        }

        private uint expirationTimeLeft;

        public bool Soulbound
        {
            get => soulbound;
            private set
            {
                if (soulbound == value)
                    return;

                soulbound = value;
                saveMask |= ItemSaveMask.Soulbound;
            }
        }

        private bool soulbound;

        /// <summary>
        /// Returns if <see cref="IItem"/> is enqueued to be saved to the database.
        /// </summary>
        public bool PendingCreate => (saveMask & ItemSaveMask.Create) != 0;

        public bool PendingDelete => (saveMask & ItemSaveMask.Delete) != 0;

        private ItemSaveMask saveMask;
        private readonly IGameTableManager gameTableManager;

        public Dictionary<Property, float> InnateProperties { get; private set; } = new Dictionary<Property, float>();

        /// <summary>
        /// Create a new <see cref="IItem"/> from an existing database model.
        /// </summary>
        public Item(
            ItemModel model,
            IItemManager itemManager = null,
            IGameTableManager gameTableManager = null)
        {
            Guid             = model.Id;
            characterId      = model.OwnerId;
            location         = InventoryLocation.None;
            PreviousLocation = InventoryLocation.None;
            bagIndex         = 0u;
            PreviousBagIndex = 0u;
            stackCount       = model.StackCount;
            durability       = model.Durability;
            expirationTimeLeft = model.ExpirationTimeLeft;
            soulbound        = model.Soulbound;
            this.gameTableManager = gameTableManager;

            if ((InventoryLocation)model.Location != InventoryLocation.Ability)
                Info = itemManager?.GetItemInfo(model.ItemId);
            else
                SpellEntry = gameTableManager?.Spell4Base.GetEntry(model.ItemId);

            charges = GetInitialCharges(Info, stackCount, model.Charges);

            ItemMicrochipIdsCodec.DeserializeInto(model.MicrochipIds, MicrochipIds);
            ItemRuneSlotsCodec.DeserializeInto(model.RuneSlots, RuneSlots);

            saveMask = ItemSaveMask.None;
        }

        /// <summary>
        /// Create a new <see cref="IItem"/> from an <see cref="IItemInfo"/> template.
        /// </summary>
        public Item(
            ulong? owner,
            IItemInfo info,
            uint count = 1u,
            uint initialCharges = 0,
            IItemManager itemManager = null,
            IGameTableManager gameTableManager = null)
        {
            Guid             = itemManager?.NextItemId ?? 0ul;
            characterId      = owner;
            location         = InventoryLocation.None;
            PreviousLocation = InventoryLocation.None;
            bagIndex         = 0u;
            stackCount       = count;
            durability       = 1.0f;
            expirationTimeLeft = GetInitialExpirationTimeLeft(info);
            soulbound        = false;
            Info             = info;
            charges          = GetInitialCharges(Info, stackCount, initialCharges);
            this.gameTableManager = gameTableManager;

            ItemRuneSlotInitializer.ApplyDefaultSockets(this, gameTableManager);

            saveMask         = ItemSaveMask.Create;
        }

        /// <summary>
        /// Create a new <see cref="IItem"/> from a <see cref="Spell4BaseEntry"/> template.
        /// </summary>
        public Item(
            ulong owner,
            Spell4BaseEntry entry,
            uint count = 1u,
            IItemManager itemManager = null,
            IGameTableManager gameTableManager = null)
        {
            Guid             = itemManager?.NextItemId ?? 0ul;
            characterId      = owner;
            location         = InventoryLocation.None;
            PreviousLocation = InventoryLocation.None;
            bagIndex         = 0u;
            PreviousBagIndex = 0u;
            stackCount       = count;
            charges          = 0u;
            durability       = 0.0f;
            soulbound        = false;
            SpellEntry       = entry;
            this.gameTableManager = gameTableManager;

            saveMask         = ItemSaveMask.Create;
        }

        /// <summary>
        /// Enqueue <see cref="IItem"/> to be deleted from the database.
        /// </summary>
        public void EnqueueDelete(bool state)
        {
            saveMask = ItemSaveMask.Delete;
        }

        public void MakeSoulbound()
        {
            Soulbound = true;
        }

        public void TouchRuneSlots()
        {
            saveMask |= ItemSaveMask.RuneSlots;
        }

        public void Save(CharacterContext context)
        {
            if (saveMask == ItemSaveMask.None)
                return;

            if (Location == InventoryLocation.RealmBank)
            {
                saveMask = ItemSaveMask.None;
                return;
            }

            if ((saveMask & ItemSaveMask.Create) != 0)
            {
                // item doesn't exist in database, all information must be saved
                context.Add(new ItemModel
                {
                    Id                 = Guid,
                    OwnerId            = CharacterId,
                    ItemId             = Id,
                    Location           = (ushort)Location,
                    BagIndex           = BagIndex,
                    StackCount         = StackCount,
                    Charges            = Charges,
                    Durability         = Durability,
                    ExpirationTimeLeft = ExpirationTimeLeft,
                    Soulbound          = Soulbound,
                    MicrochipIds       = ItemMicrochipIdsCodec.Serialize(MicrochipIds),
                    RuneSlots          = ItemRuneSlotsCodec.Serialize(RuneSlots)
                });
            }
            else if ((saveMask & ItemSaveMask.Delete) != 0)
            {
                var model = new ItemModel
                {
                    Id = Guid,
                };

                context.Entry(model).State = EntityState.Deleted;
            }
            else
            {
                // item already exists in database, save only data that has been modified
                var model = new ItemModel
                {
                    Id = Guid
                };

                // could probably clean this up with reflection, works for the time being
                EntityEntry<ItemModel> entity = context.Attach(model);
                if ((saveMask & ItemSaveMask.CharacterId) != 0)
                {
                    model.OwnerId = CharacterId;
                    entity.Property(p => p.OwnerId).IsModified = true;
                }
                if ((saveMask & ItemSaveMask.Location) != 0)
                {
                    model.Location = (ushort)Location;
                    entity.Property(p => p.Location).IsModified = true;
                }
                if ((saveMask & ItemSaveMask.BagIndex) != 0)
                {
                    model.BagIndex = BagIndex;
                    entity.Property(p => p.BagIndex).IsModified = true;
                }
                if ((saveMask & ItemSaveMask.StackCount) != 0)
                {
                    model.StackCount = StackCount;
                    entity.Property(p => p.StackCount).IsModified = true;
                }
                if ((saveMask & ItemSaveMask.Charges) != 0)
                {
                    model.Charges = Charges;
                    entity.Property(p => p.Charges).IsModified = true;
                }
                if ((saveMask & ItemSaveMask.Durability) != 0)
                {
                    model.Durability = Durability;
                    entity.Property(p => p.Durability).IsModified = true;
                }
                if ((saveMask & ItemSaveMask.ExpirationTimeLeft) != 0)
                {
                    model.ExpirationTimeLeft = ExpirationTimeLeft;
                    entity.Property(p => p.ExpirationTimeLeft).IsModified = true;
                }
                if ((saveMask & ItemSaveMask.Soulbound) != 0)
                {
                    model.Soulbound = Soulbound;
                    entity.Property(p => p.Soulbound).IsModified = true;
                }
                model.MicrochipIds = ItemMicrochipIdsCodec.Serialize(MicrochipIds);
                entity.Property(p => p.MicrochipIds).IsModified = true;

                model.RuneSlots = ItemRuneSlotsCodec.Serialize(RuneSlots);
                entity.Property(p => p.RuneSlots).IsModified = true;
            }

            saveMask = ItemSaveMask.None;
        }

        /// <summary>
        /// Return a network representation of the current <see cref="IItem"/> for use in a item related packet.
        /// </summary>
        public NetworkItem Build()
        {
            EnsureInitialCharges();
            ItemRuneSlotInitializer.ApplyDefaultSockets(this, gameTableManager);

            var networkItem = new NetworkItem
            {
                ItemGuid     = Guid,
                Item2Id      = Id,
                LocationData = new ItemLocation
                {
                    Location = Location,
                    BagIndex = BagIndex
                },
                StackCount = StackCount,
                Charges    = Charges,
                Durability = Durability,
                DynamicFlags = Soulbound ? DynamicItemFlags.Soulbound : DynamicItemFlags.None,
                ExpirationTimeLeft = ExpirationTimeLeft,
                SellPrices  = new NetworkItem.PriceInfo[2]
                {
                    new NetworkItem.PriceInfo(),
                    new NetworkItem.PriceInfo()
                }
            };

            ItemRuneNetworkWire.Populate(networkItem, this, gameTableManager);

            return networkItem;
        }

        /// <summary>
        /// Returns the <see cref="CurrencyType"/> this <see cref="IItem"/> sells for at a vendor.
        /// </summary>
        public CurrencyType GetVendorSellCurrency(byte index)
        {
            return Info.GetVendorSellCurrency(index);
        }

        /// <summary>
        /// Returns the amount of <see cref="CurrencyType"/> this <see cref="IItem"/> sells for at a vendor.
        /// </summary>
        public uint GetVendorSellAmount(byte index)
        {
            return Info.GetVendorSellAmount(index);
        }

        private static uint GetInitialExpirationTimeLeft(IItemInfo info)
        {
            if (info?.Entry.ExpirationTimeMinutes is null or 0u)
                return 0u;

            ulong seconds = (ulong)info.Entry.ExpirationTimeMinutes * 60ul;
            return seconds > uint.MaxValue ? uint.MaxValue : (uint)seconds;
        }

        private static uint GetInitialCharges(IItemInfo info, uint stackCount, uint initialCharges)
        {
            if (initialCharges != 0u || stackCount == 0u || info?.Entry?.MaxCharges is null or 0u)
                return initialCharges;

            return info.Entry.MaxCharges;
        }

        private void EnsureInitialCharges()
        {
            if (charges != 0u || stackCount == 0u || Info?.Entry?.MaxCharges is null or 0u)
                return;

            charges = Info.Entry.MaxCharges;
            saveMask |= ItemSaveMask.Charges;
        }
    }
}
