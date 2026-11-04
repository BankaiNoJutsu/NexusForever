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
    public partial class Item : IItem
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
                lock (persistenceGate)
                {
                    EnsureRewardMutationAllowed();
                    characterId = value;
                    MarkDirty(ItemSaveMask.CharacterId);
                }
            }
        }

        private ulong? characterId;

        public InventoryLocation Location
        {
            get => location;
            set
            {
                lock (persistenceGate)
                {
                    EnsureRewardMutationAllowed();
                    location = value;
                    MarkDirty(ItemSaveMask.Location);
                }
            }
        }

        private InventoryLocation location;

        public InventoryLocation PreviousLocation { get; set; }

        public uint BagIndex
        {
            get => bagIndex;
            set
            {
                lock (persistenceGate)
                {
                    EnsureRewardMutationAllowed();
                    bagIndex = value;
                    MarkDirty(ItemSaveMask.BagIndex);
                }
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

                lock (persistenceGate)
                {
                    EnsureRewardMutationAllowed();
                    stackCount = value;
                    MarkDirty(ItemSaveMask.StackCount);
                }
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

                lock (persistenceGate)
                {
                    EnsureRewardMutationAllowed();
                    charges = value;
                    MarkDirty(ItemSaveMask.Charges);
                }
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

                lock (persistenceGate)
                {
                    EnsureMarketplaceMutationAllowed();
                    durability = value;
                    MarkDirty(ItemSaveMask.Durability);
                }
            }
        }

        private float durability;

        public IList<uint> MicrochipIds
        {
            get { lock (persistenceGate) return microchipIds ??= new MarketplaceCollection<uint>(this); }
        }

        public IList<ItemRuneSlot> RuneSlots
        {
            get { lock (persistenceGate) return runeSlots ??= new MarketplaceCollection<ItemRuneSlot>(this); }
        }

        public uint ExpirationTimeLeft
        {
            get => expirationTimeLeft;
            set
            {
                lock (persistenceGate)
                {
                    EnsureRewardMutationAllowed();
                    expirationTimeLeft = value;
                    MarkDirty(ItemSaveMask.ExpirationTimeLeft);
                }
            }
        }

        private uint expirationTimeLeft;

        public bool Soulbound
        {
            get => soulbound;
            private set
            {
                lock (persistenceGate)
                {
                    if (soulbound == value)
                        return;

                    EnsureRewardMutationAllowed();
                    soulbound = value;
                    MarkDirty(ItemSaveMask.Soulbound);
                }
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
            persistenceBaseline = (InventoryLocation)model.Location == InventoryLocation.RealmBank ? null : CopyModel(model);
            realmBankBaseline = (InventoryLocation)model.Location == InventoryLocation.RealmBank ? CopyModel(model) : null;
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
            lock (persistenceGate)
            {
                EnsureRewardMutationAllowed();
                deleteRequested = state;
                MarkDirty(ItemSaveMask.Delete);
                if (!state)
                    saveMask &= ~ItemSaveMask.Delete;
            }
        }

        public void MakeSoulbound()
        {
            Soulbound = true;
        }

        public void TouchRuneSlots()
        {
            lock (persistenceGate)
            {
                EnsureMarketplaceMutationAllowed();
                MarkDirty(ItemSaveMask.RuneSlots);
            }
        }

        public void Save(CharacterContext context) => SavePersistence(context, false);

        /// <summary>
        /// Return a network representation of the current <see cref="IItem"/> for use in a item related packet.
        /// </summary>
        public NetworkItem Build()
        {
            // Marketplace delivery initializes template sockets before the ordinary
            // save. Publication must not accept new live mutations after that save.
            if (marketplacePublication == null)
            {
                EnsureInitialCharges();
                ItemRuneSlotInitializer.ApplyDefaultSockets(this, gameTableManager);
            }

            return BuildNetworkItem();
        }

        internal NetworkItem BuildNetworkItem()
        {
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
                Charges    = GetInitialCharges(Info, StackCount, Charges),
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
            lock (persistenceGate)
            {
                if (charges != 0u || stackCount == 0u || Info?.Entry?.MaxCharges is null or 0u)
                    return;

                EnsureRewardMutationAllowed();
                charges = Info.Entry.MaxCharges;
                MarkDirty(ItemSaveMask.Charges);
            }
        }
    }
}
