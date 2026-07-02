using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Item;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using ItemModel = NexusForever.Network.World.Message.Model.Item.Item;
using ItemDragDropModel = NexusForever.Network.World.Message.Model.Item.ItemDragDrop;
using ItemInventoryId = NexusForever.Network.World.Message.Model.Item.InventoryId;
using ItemLocationModel = NexusForever.Network.World.Message.Model.Item.ItemLocation;
using ItemRuneSlots = NexusForever.Network.World.Message.Model.Item.RuneSlots;
using ItemServerFlags = NexusForever.Network.World.Message.Model.Item.ServerItemFlags;
using ItemTradePartners = NexusForever.Network.World.Message.Model.Item.ServerItemTradePartners;

namespace NexusForever.Game.Tests.Network;

public class ItemProtocolPacketShapeTests
{
    [Fact]
    public void InventoryId_WriteRoundTripsPackedLocationSlotAndCount()
    {
        ItemInventoryId inventoryId = ItemInventoryId.FromLocation(InventoryLocation.GuildBankTab10, 0x7F, 3);

        ItemInventoryId roundTrip = ReadPacket<ItemInventoryId>(WritePacket(inventoryId));

        Assert.Equal((byte)0x7F, roundTrip.BagSlot);
        Assert.Equal(InventoryLocation.GuildBankTab10, roundTrip.Location);
        Assert.Equal((ushort)3, roundTrip.Count);
    }

    [Fact]
    public void InventoryId_FromLocationRejectsSlotOutsideByte()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ItemInventoryId.FromLocation(InventoryLocation.Inventory, 0x100));
    }

    [Fact]
    public void ItemDragDrop_WriteWritesPackedInventoryId()
    {
        var dragDrop = new ItemDragDropModel
        {
            ItemGuid = 0x0102030405060708ul,
            DragDrop = ItemInventoryId.FromLocation(InventoryLocation.RealmBank, 0x22, 5)
        };

        using var stream = new MemoryStream(WritePacket(dragDrop));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0x0000000000050A22ul, reader.ReadULong());
    }

    [Fact]
    public void ItemLocation_WriteRejectsInventoryLocationOutsideNineBitWireLimit()
    {
        var itemLocation = new ItemLocationModel
        {
            Location = InventoryLocation.None
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => WritePacket(itemLocation));
    }

    [Theory]
    [InlineData(InventoryLocation.SharedBank, 10u)]
    [InlineData(InventoryLocation.GuildBankTab10, 109u)]
    [InlineData(InventoryLocation.WarPartyBankTab10, 209u)]
    public void ItemLocation_WritePreservesExpandedLocationsInsideNineBitWireLimit(InventoryLocation location, uint expectedWireLocation)
    {
        var itemLocation = new ItemLocationModel
        {
            Location = location,
            BagIndex = 0x11223344u
        };

        using var stream = new MemoryStream(WritePacket(itemLocation));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(expectedWireLocation, reader.ReadUInt(9u));
        Assert.Equal(0x11223344u, reader.ReadUInt());
    }

    [Fact]
    public void ItemModel_WriteRejectsItemIdOutsideEighteenBitWireLimit()
    {
        var item = new ItemModel
        {
            Item2Id = 1u << 18
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => WritePacket(item));
    }

    [Fact]
    public void ServerItemFlags_WriteRejectsFlagsOutsideEightBitPatchLimit()
    {
        var update = new ItemServerFlags
        {
            Flags = (ItemFlags)0x100u
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => WritePacket(update));
    }

    [Fact]
    public void ServerItemTradePartners_WriteRejectsCountOutsideSixBitLimit()
    {
        var update = new ItemTradePartners();
        for (int i = 0; i < 64; i++)
            update.TradingPartnerIdentities.Add(new());

        Assert.Throws<ArgumentOutOfRangeException>(() => WritePacket(update));
    }

    [Fact]
    public void RuneSlots_RoundTripsAllCraftingGroupsIncludingIndexThree()
    {
        var runeSlots = new ItemRuneSlots
        {
            Unknown1 = 5,
            Unknown2 = 6,
            Unknown3 = true,
            CraftingGroup = [0, 1, 2, 3, 4, 5, 6, 7]
        };

        ItemRuneSlots roundTrip = ReadPacket<ItemRuneSlots>(WritePacket(runeSlots));

        Assert.Equal(runeSlots.Unknown1, roundTrip.Unknown1);
        Assert.Equal(runeSlots.Unknown2, roundTrip.Unknown2);
        Assert.Equal(runeSlots.Unknown3, roundTrip.Unknown3);
        Assert.Equal(runeSlots.CraftingGroup, roundTrip.CraftingGroup);
    }

    [Fact]
    public void ClientVendorPurchase_ReadNamesStockUniqueIdAndPurchaseQuantity()
    {
        ClientVendorPurchase purchase = ReadPacket<ClientVendorPurchase>(WritePacket(writer =>
        {
            writer.Write(0x01020304u);
            writer.Write(0x05060708u);
        }));

        Assert.Equal(0x01020304u, purchase.StockUniqueId);
        Assert.Equal(0x05060708u, purchase.PurchaseQuantity);
    }

    [Fact]
    public void ServerVendorItemsUpdated_WriteUsesVendorGroupsItemsAndExtraCosts()
    {
        var update = new ServerVendorItemsUpdated
        {
            VendorUnitId        = 0x01020304u,
            SellPriceMultiplier = 1.5f,
            BuyPriceMultiplier  = 0.75f,
            InitialList         = true,
            Failed              = false,
            ClearList           = true
        };

        update.VendorGroups.Add(new ServerVendorItemsUpdated.VendorGroup
        {
            GroupIndex      = 0x11u,
            LocalisedTextId = 0x22u
        });

        update.VendorItems.Add(new ServerVendorItemsUpdated.VendorItem
        {
            StockUniqueId  = 0x33u,
            VendorItemType = 0x0A,
            StaticDbId     = 0x44u,
            RewardOption   = 0x55u,
            StockCount     = 0x66u,
            PrerequisiteId = 0x1ABCDu,
            ExchangeRate   = 0x77u,
            VendorGroupId  = 0x88u,
            Flags          = 0x99u,
            CircuitData    = 0x0102030405060708ul,
            GlyphData      = 0x11223344u,
            ExtraCost1     = new ServerVendorItemsUpdated.VendorItem.ItemExtraCost
            {
                ExtraCostType    = ItemExtraCostType.Item,
                Quantity         = 2u,
                ItemOrCurrencyId = 3u
            },
            ExtraCost2 = new ServerVendorItemsUpdated.VendorItem.ItemExtraCost
            {
                ExtraCostType    = ItemExtraCostType.AccountCurrency,
                Quantity         = 4u,
                ItemOrCurrencyId = 5u
            }
        });

        using var stream = new MemoryStream(WritePacket(update));
        using var reader = new GamePacketReader(stream);

        Assert.Equal(0x01020304u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x11u, reader.ReadUInt());
        Assert.Equal(0x22u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(0x33u, reader.ReadUInt());
        Assert.Equal((byte)0x0A, reader.ReadByte(4u));
        Assert.Equal(0x44u, reader.ReadUInt());
        Assert.Equal(0x55u, reader.ReadUInt());
        Assert.Equal(0x66u, reader.ReadUInt());
        Assert.Equal(0x1ABCDu, reader.ReadUInt(17u));
        Assert.Equal(0x77u, reader.ReadUInt());
        Assert.Equal(0x88u, reader.ReadUInt());
        Assert.Equal(0x99u, reader.ReadUInt());
        Assert.Equal(0x0102030405060708ul, reader.ReadULong());
        Assert.Equal(0x11223344u, reader.ReadUInt());
        Assert.Equal(ItemExtraCostType.Item, reader.ReadEnum<ItemExtraCostType>(3u));
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal(3u, reader.ReadUInt());
        Assert.Equal(ItemExtraCostType.AccountCurrency, reader.ReadEnum<ItemExtraCostType>(3u));
        Assert.Equal(4u, reader.ReadUInt());
        Assert.Equal(5u, reader.ReadUInt());
        Assert.Equal(1.5f, reader.ReadSingle());
        Assert.Equal(0.75f, reader.ReadSingle());
        Assert.True(reader.ReadBit());
        Assert.False(reader.ReadBit());
        Assert.True(reader.ReadBit());
    }

    private static T ReadPacket<T>(byte[] packetData) where T : IReadable, new()
    {
        using var stream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(stream);

        var packet = new T();
        packet.Read(reader);
        return packet;
    }

    private static byte[] WritePacket(IWritable packet)
    {
        return WritePacket(packet.Write);
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
