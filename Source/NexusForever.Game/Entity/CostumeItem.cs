using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;

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
        /// Return dye ramp mask generated from supplied dyes.
        /// </summary>
        public static uint GenerateDyeMask(uint[] dyes, IGameTableManager gameTableManager)
        {
            uint[] ramps = new uint[MaxCostumeItemDyes];
            for (var i = 0; i < dyes.Length; i++)
            {
                if (dyes[i] == 0)
                    continue;

                DyeColorRampEntry entry = gameTableManager.DyeColorRamp?.GetEntry(dyes[i]);
                if (entry == null)
                    throw new ArgumentException($"Unknown dye color ramp {dyes[i]}.", nameof(dyes));

                ramps[i] = entry.RampIndex;
            }

            return ((ramps[2] & 0x3FFu) | 0xFFFFF800u) << 20
                | (ramps[1] & 0x3FFu) << 10
                | ramps[0] & 0x3FFu;
        }

        public CostumeItemSlot Slot { get; }
        public ItemSlot ItemSlot { get; }
        public IItemInfo ItemInfo { get; private set; }

        public uint? ItemId
        {
            get => ItemInfo?.Id;
            set
            {
                if (ItemInfo?.Id == value)
                    return;

                ItemInfo  = value.HasValue ? itemManager?.GetItemInfo(value.Value) : null;
                saveMask |= CostumeItemSaveMask.ItemId;
            }
        }

        public ushort? DisplayId => ItemInfo?.GetDisplayId();

        public uint DyeData
        {
            get => dyeData;
            set
            {
                if (dyeData == value)
                    return;

                dyeData = value;
                saveMask |= CostumeItemSaveMask.DyeData;
            }
        }

        private uint dyeData;

        private readonly ICostume costume;
        private readonly IItemManager itemManager;
        private readonly IGameTableManager gameTableManager;

        private CostumeItemSaveMask saveMask;

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
            ItemId       = model.Item2Id > 0 ? model.Item2Id : null;
            dyeData      = model.DyeData;

            saveMask     = CostumeItemSaveMask.None;
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
            ItemId       = item.ItemId > 0 ? item.ItemId : null;
            dyeData      = GenerateDyeMask(item.Dyes, gameTableManager);

            saveMask     = CostumeItemSaveMask.Create;
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

        public void Save(CharacterContext context)
        {
            if (saveMask == CostumeItemSaveMask.None)
                return;

            if ((saveMask & CostumeItemSaveMask.Create) != 0)
            {
                // costume item doesn't exist in database, all infomation must be saved
                context.Add(new CharacterCostumeItemModel
                {
                    Id      = costume.Owner,
                    Index   = costume.Index,
                    Slot    = (byte)Slot,
                    Item2Id = ItemId ?? 0,
                    DyeData = dyeData
                });
            }
            else
            {
                // costume item already exists in database, save only data that has been modified
                var model = new CharacterCostumeItemModel
                {
                    Id    = costume.Owner,
                    Index = costume.Index,
                    Slot  = (byte)Slot
                };

                EntityEntry<CharacterCostumeItemModel> entity = context.Attach(model);
                if ((saveMask & CostumeItemSaveMask.ItemId) != 0)
                {
                    model.Item2Id = ItemId ?? 0;
                    entity.Property(p => p.Item2Id).IsModified = true;
                }
                if ((saveMask & CostumeItemSaveMask.DyeData) != 0)
                {
                    model.DyeData = dyeData;
                    entity.Property(p => p.DyeData).IsModified = true;
                }
            }

            saveMask = CostumeItemSaveMask.None;
        }

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
