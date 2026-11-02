using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Costume;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Costume;

namespace NexusForever.Game.Entity
{
    public class CostumeItem : ICostumeItem
    {
        /// <summary>
        /// Determines which fields need saving for <see cref="ICostumeItem"/> when being saved to the database.
        /// </summary>
        [Flags]
        public enum CostumeItemSaveMask
        {
            None    = 0x00,
            Create  = 0x01,
            ItemId  = 0x02,
            DyeData = 0x04
        }

        public const byte MaxCostumeItemDyes = 3;

        /// <summary>
        /// Returns packed dye RampIndexes generated from supplied dyeColorRampIds.
        /// </summary>
        public static uint GenerateDyeData(uint[] dyeColorRampIds, IGameTableManager gameTableManager)
        {
            uint[] ramps = new uint[MaxCostumeItemDyes];
            for (var i = 0; i < dyeColorRampIds.Length; i++)
            {
                if (dyeColorRampIds[i] == 0)
                    continue;

                DyeColorRampEntry entry = gameTableManager?.DyeColorRamp?.GetEntry(dyeColorRampIds[i]);
                if (entry == null)
                    throw new ArgumentException($"Unknown dye color ramp {dyeColorRampIds[i]}.", nameof(dyeColorRampIds));

                ramps[i] = entry.RampIndex;
            }

            return ((ramps[2] & 0x3FFu) | 0xFFFFF800u) << 20
                | (ramps[1] & 0x3FFu) << 10
                | ramps[0] & 0x3FFu;
        }

        public CostumeItemSlot Slot { get; }
        public ItemSlot ItemSlot { get; }
        public IItemInfo ItemInfo { get; private set; }

        public uint? Item2Id
        {
            get => ItemInfo?.Id;
            set
            {
                lock (persistence.Gate)
                {
                    if (ItemInfo?.Id == value) return;
                    ItemInfo = value.HasValue ? itemManager?.GetItemInfo(value.Value) : null;
                    storedItemId = ItemInfo?.Id ?? 0;
                }
            }
        }

        public ushort? DisplayId => ItemInfo?.GetDisplayId();

        public uint DyeData
        {
            get { lock (persistence.Gate) return dyeData; }
            set
            {
                lock (persistence.Gate) dyeData = value;
            }
        }

        private uint dyeData;

        private readonly ICostume costume;
        private readonly IItemManager itemManager;
        private readonly IGameTableManager gameTableManager;

        private readonly CostumePersistence persistence;
        private readonly ulong owner;
        private readonly byte index;
        private uint storedItemId;

        /// <summary>
        /// Create a new <see cref="ICostumeItem"/> from an existing <see cref="CharacterCostumeItemModel"/> database model.
        /// </summary>
        public CostumeItem(
            ICostume costume,
            CharacterCostumeItemModel model,
            IItemManager itemManager = null,
            IGameTableManager gameTableManager = null)
        {
            this.costume = costume;
            this.itemManager = itemManager;
            this.gameTableManager = gameTableManager;
            Slot         = (CostumeItemSlot)model.Slot;
            ItemSlot     = GetSlot(Slot);
            owner = costume.Owner; index = costume.Index;
            storedItemId = model.Item2Id;
            ItemInfo = model.Item2Id > 0 ? itemManager?.GetItemInfo(model.Item2Id) : null;
            persistence = costume is Costume concrete ? concrete.Persistence
                : new(owner, new(null, [], [new(index, (byte)Slot, model.Item2Id, model.DyeData)]), CaptureSlot, index: index, slot: (byte)Slot);
            dyeData      = model.DyeData;

        }

        /// <summary>
        /// Create a new <see cref="ICostumeItem"/> from packet <see cref="ClientCostumeSave"/>.
        /// </summary>
        public CostumeItem(
            ICostume costume,
            ClientCostumeSave.CostumeItem item,
            CostumeItemSlot slot,
            IItemManager itemManager = null,
            IGameTableManager gameTableManager = null)
        {
            this.costume = costume;
            this.itemManager = itemManager;
            this.gameTableManager = gameTableManager;
            Slot         = slot;
            ItemSlot     = GetSlot(Slot);
            owner = costume.Owner; index = costume.Index;
            persistence = costume is Costume concrete ? concrete.Persistence
                : new(owner, new(null, [], []), CaptureSlot, index: index, slot: (byte)Slot);
            Item2Id      = item.Item2Id > 0 ? item.Item2Id : null;
            dyeData      = GenerateDyeData(item.DyeColorRampIds, gameTableManager);

        }

        private static ItemSlot GetSlot(CostumeItemSlot slot)
        {
            return slot switch
            {
                CostumeItemSlot.Chest    => ItemSlot.ArmorChest,
                CostumeItemSlot.Legs     => ItemSlot.ArmorLegs,
                CostumeItemSlot.Head     => ItemSlot.ArmorHead,
                CostumeItemSlot.Shoulder => ItemSlot.ArmorShoulder,
                CostumeItemSlot.Feet     => ItemSlot.ArmorFeet,
                CostumeItemSlot.Hands    => ItemSlot.ArmorHands,
                CostumeItemSlot.Weapon   => ItemSlot.WeaponPrimary,
                _                        => throw new ArgumentOutOfRangeException(nameof(slot))
            };
        }

        public void Save(CharacterContext context) => persistence.SaveSlot(context, index, (byte)Slot);

        internal CostumePersistence.Slot CapturePersistence()
        {
            if (costume.Owner != owner || costume.Index != index) throw new InvalidOperationException("Costume item changed its owner.");
            return new(index, (byte)Slot, storedItemId, dyeData);
        }
        private CostumePersistence.Snapshot CaptureSlot() => new(null, [], [CapturePersistence()]);

        /// <summary>
        /// Get <see cref="IItemVisual"/> for <see cref="ICostumeItem"/>.
        /// </summary>
        public IItemVisual GetItemVisual()
        {
            return new ItemVisual
            {
                Slot      = ItemSlot,
                DisplayId = DisplayId,
                DyeData   = DyeData
            };
        }
    }
}
