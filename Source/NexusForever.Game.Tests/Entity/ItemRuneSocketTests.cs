using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NetworkItem = NexusForever.Network.World.Message.Model.Shared.Item;

namespace NexusForever.Game.Tests.Entity;

public class ItemRuneSocketTests
{
    [Theory]
    [InlineData(7u, RuneType.Air)]
    [InlineData(13u, RuneType.Fusion)]
    [InlineData(1u, RuneType.Air)]
    public void TryToRuneType_AcceptsFullAndCompactIds(uint input, RuneType expected)
    {
        Assert.True(ItemRuneSocketTypes.TryToRuneType(input, out RuneType runeType));
        Assert.Equal(expected, runeType);
    }

    [Fact]
    public void MapRuneTypeToBit_MatchesPrerequisiteMapping()
    {
        Assert.Equal(8u, ItemRuneSocketTypes.MapRuneTypeToBit(RuneType.Fire));
        Assert.Equal(0x40u, ItemRuneSocketTypes.MapRuneTypeToBit(RuneType.Fusion));
    }

    [Fact]
    public void ApplyDefaultSockets_SeedsFromItemRuneInstance()
    {
        IGameTableManager tables = CreateItemRuneInstanceTables();
        IItem item = CreateItemWithRuneInstance(2u);

        ItemRuneSlotInitializer.ApplyDefaultSockets(item, tables);

        Assert.Single(item.RuneSlots);
        Assert.Equal(RuneType.Air, item.RuneSlots[0].Type);
        Assert.Equal(0u, item.RuneSlots[0].RuneItem2Id);
    }

    [Fact]
    public void ApplyDefaultSockets_BrutalRunicShouldersSeedsDefinedSocketOnly()
    {
        IGameTableManager tables = CreateItemRuneInstanceTables();
        IItem item = CreateItemWithRuneInstance(139u, 84804u);

        ItemRuneSlotInitializer.ApplyDefaultSockets(item, tables);

        Assert.Single(item.RuneSlots);
        Assert.Equal(RuneType.Life, item.RuneSlots[0].Type);
    }

    [Fact]
    public void ApplyDefaultSockets_MissingItemRuneInstanceTable_DoesNotThrow()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        IItem item = CreateItemWithRuneInstance(2u);

        ItemRuneSlotInitializer.ApplyDefaultSockets(item, tables);

        Assert.Empty(item.RuneSlots);
    }

    [Fact]
    public void TryGetRuneTypeFromSigilItem2Type_MapsCategory176Glyphs()
    {
        Assert.True(ItemRuneGlyphTypes.TryGetRuneTypeFromSigilItem2Type(419u, out RuneType air));
        Assert.Equal(RuneType.Air, air);
        Assert.True(ItemRuneGlyphTypes.TryGetRuneTypeFromSigilItem2Type(424u, out RuneType life));
        Assert.Equal(RuneType.Life, life);
        Assert.False(ItemRuneGlyphTypes.TryGetRuneTypeFromSigilItem2Type(352u, out _));
    }

    [Theory]
    [InlineData(ItemRuneGlyphTypes.Category175Id, 352u, RuneType.Fire)]
    [InlineData(ItemRuneGlyphTypes.Category175Id, 353u, RuneType.Water)]
    [InlineData(ItemRuneGlyphTypes.Category175Id, 354u, RuneType.Earth)]
    [InlineData(ItemRuneGlyphTypes.Category175Id, 355u, RuneType.Air)]
    [InlineData(ItemRuneGlyphTypes.Category175Id, 356u, RuneType.Life)]
    [InlineData(ItemRuneGlyphTypes.Category175Id, 357u, RuneType.Logic)]
    [InlineData(ItemRuneGlyphTypes.Category175Id, 358u, RuneType.Fusion)]
    [InlineData(ItemRuneGlyphTypes.Category177Id, 339u, RuneType.Fire)]
    [InlineData(ItemRuneGlyphTypes.Category177Id, 340u, RuneType.Water)]
    [InlineData(ItemRuneGlyphTypes.Category177Id, 341u, RuneType.Earth)]
    [InlineData(ItemRuneGlyphTypes.Category177Id, 342u, RuneType.Air)]
    [InlineData(ItemRuneGlyphTypes.Category177Id, 343u, RuneType.Life)]
    [InlineData(ItemRuneGlyphTypes.Category177Id, 344u, RuneType.Logic)]
    [InlineData(ItemRuneGlyphTypes.Category177Id, 345u, RuneType.Fusion)]
    [InlineData(ItemRuneGlyphTypes.Category177Id, 515u, RuneType.Fusion)]
    [InlineData(ItemRuneGlyphTypes.Category177Id, 516u, RuneType.Fusion)]
    [InlineData(ItemRuneGlyphTypes.Category177Id, 530u, RuneType.Fusion)]
    public void TryGetRuneTypeFromRunecraftingGlyph_MapsAdditionalCategoryBands(uint categoryId, uint typeId, RuneType expected)
    {
        Assert.True(ItemRuneGlyphTypes.TryGetRuneTypeFromRunecraftingGlyph(categoryId, typeId, out RuneType runeType));
        Assert.Equal(expected, runeType);
    }

    [Fact]
    public void ValidateRuneMatchesSocket_AcceptsMatchingSigilType()
    {
        IGameTableManager tables = CreateRunecraftingGlyphTables(900u, 420u);

        IItem item = CreateGearWithSocket(RuneType.Water);
        TradeskillResult result = ItemRuneInstallValidator.ValidateRuneMatchesSocket(tables, item, RuneType.Water, 900u);

        Assert.Equal(TradeskillResult.Success, result);
    }

    [Fact]
    public void ValidateRuneMatchesSocket_AcceptsBeginnerLifeRuneType()
    {
        IGameTableManager tables = CreateRunecraftingGlyphTables(84956u, 356u, ItemRuneGlyphTypes.Category175Id);

        IItem item = CreateGearWithSocket(RuneType.Life);
        TradeskillResult result = ItemRuneInstallValidator.ValidateRuneMatchesSocket(tables, item, RuneType.Life, 84956u);

        Assert.Equal(TradeskillResult.Success, result);
    }

    [Fact]
    public void ValidateRuneMatchesSocket_RejectsMismatchedElement()
    {
        IGameTableManager tables = CreateRunecraftingGlyphTables(900u, 422u);

        IItem item = CreateGearWithSocket(RuneType.Water);
        TradeskillResult result = ItemRuneInstallValidator.ValidateRuneMatchesSocket(tables, item, RuneType.Water, 900u);

        Assert.Equal(TradeskillResult.InvalidSlot, result);
    }

    [Fact]
    public void ValidateRuneMatchesSocket_RejectsGlyphOnWrongElementSocketWhenGearHasOtherElements()
    {
        IGameTableManager tables = CreateRunecraftingGlyphTables(901u, 421u);

        IItem item = CreateGearWithSockets(RuneType.Air, RuneType.Fire);
        TradeskillResult result = ItemRuneInstallValidator.ValidateRuneMatchesSocket(tables, item, RuneType.Air, 901u);

        Assert.Equal(TradeskillResult.InvalidSlot, result);
    }

    [Fact]
    public void ValidateRuneMatchesSocket_FusionSocket_AcceptsAnyRunecraftingGlyph()
    {
        IGameTableManager tables = CreateRunecraftingGlyphTables(900u, 422u);

        IItem item = CreateGearWithSocket(RuneType.Fusion);
        TradeskillResult result = ItemRuneInstallValidator.ValidateRuneMatchesSocket(tables, item, RuneType.Fusion, 900u);

        Assert.Equal(TradeskillResult.Success, result);
    }

    [Fact]
    public void ValidateInstallTargets_AppliesDefaultSocketsBeforeCountCheck()
    {
        IGameTableManager tables = CreateInstallValidationTables();

        IItem item = CreateItemWithRuneInstance(2u);

        TradeskillResult result = ItemRuneInstallValidator.ValidateInstallTargets(tables, item, [0u]);

        Assert.Equal(TradeskillResult.Success, result);
        Assert.Single(item.RuneSlots);
    }

    [Fact]
    public void ValidateInstallTargets_RejectsDuplicateRuneItem2Ids()
    {
        IGameTableManager tables = CreateInstallValidationTables(900u, 420u);

        IItem item = CreateGearWithSockets(RuneType.Air, RuneType.Water);

        TradeskillResult result = ItemRuneInstallValidator.ValidateInstallTargets(tables, item, [900u, 900u]);

        Assert.Equal(TradeskillResult.DuplicateRune, result);
    }

    [Fact]
    public void BuildAllowedSocketMask_UsesItemRuneInstanceWhenSlotsEmpty()
    {
        IGameTableManager tables = CreateItemRuneInstanceTables();
        IItem item = CreateItemWithRuneInstance(2u);

        uint mask = ItemRuneSocketMaskBuilder.BuildAllowedSocketMask(item, tables);

        Assert.Equal(ItemRuneSocketTypes.MapRuneTypeToBit(RuneType.Air), mask);
    }

    [Fact]
    public void BuildAllowedSocketMask_MissingItemRuneInstanceTable_DoesNotThrow()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out _);
        IItem item = CreateItemWithRuneInstance(2u);

        uint mask = ItemRuneSocketMaskBuilder.BuildAllowedSocketMask(item, tables);

        Assert.Equal(0u, mask);
    }

    [Fact]
    public void BuildAllowedSocketMask_IgnoresItemSpecialSpellFkValues()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.ItemSpecial), CreateGameTable(new ItemSpecialEntry
        {
            Id              = 9261u,
            Spell4IdOnEquip = 81886u,
        }));
        proxy.SetProperty(nameof(IGameTableManager.ItemRuneInstance), CreateGameTable<ItemRuneInstanceEntry>());

        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            ItemSpecialId00    = 9261u,
            ItemRuneInstanceId = 0u,
        });
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot>());

        uint mask = ItemRuneSocketMaskBuilder.BuildAllowedSocketMask(item, tables);

        Assert.Equal(0u, mask);
    }

    [Fact]
    public void BuildAllowedSocketMask_UnionsTemplateWithRuntimeSockets()
    {
        IGameTableManager tables = CreateInstallValidationTables();
        IItem item = CreateItemWithRuneInstance(2u);
        item.RuneSlots.Add(new ItemRuneSlot(RuneType.Fusion));

        uint mask = ItemRuneSocketMaskBuilder.BuildAllowedSocketMask(item, tables);

        uint expected = ItemRuneSocketTypes.MapRuneTypeToBit(RuneType.Air)
            | ItemRuneSocketTypes.MapRuneTypeToBit(RuneType.Fusion);
        Assert.Equal(expected, mask);
    }

    [Fact]
    public void PopulateNetworkItem_EmitsCircuitMarkerSocketTypesAndIndexAlignedGlyphs()
    {
        var networkItem = new NetworkItem();
        var slots = new List<ItemRuneSlot>
        {
            new(RuneType.Air) { RuneItem2Id = 100u },
            new(RuneType.Water),
            new(RuneType.Fusion) { RuneItem2Id = 200u },
        };

        ItemRuneNetworkWire.Populate(networkItem, slots, [], definedSocketCount: 3u);

        uint expectedRandomGlyphData = (1u << 7) | (2u << 10) | (7u << 13);
        Assert.Equal(1ul << 56, networkItem.RandomCircuitData);
        Assert.Equal(expectedRandomGlyphData, networkItem.RandomGlyphData);
        Assert.Equal([100u, 0u, 200u], networkItem.Glyphs);
        Assert.Empty(networkItem.Microchips);
    }

    [Fact]
    public void PopulateNetworkItem_PacksAddedSlotCountInRandomGlyphData()
    {
        var networkItem = new NetworkItem();
        var slots = new List<ItemRuneSlot>
        {
            new(RuneType.Life),
            new(RuneType.Fire),
        };

        ItemRuneNetworkWire.Populate(networkItem, slots, [], definedSocketCount: 1u);

        uint expectedRandomGlyphData = 1u | (6u << 7) | (4u << 10);
        Assert.Equal(1ul << 56, networkItem.RandomCircuitData);
        Assert.Equal(expectedRandomGlyphData, networkItem.RandomGlyphData);
    }

    [Fact]
    public void PopulateNetworkItem_EmptySocketsDoNotEmitCircuitMarker()
    {
        var networkItem = new NetworkItem();

        ItemRuneNetworkWire.Populate(networkItem, [], [], definedSocketCount: 0u);

        Assert.Equal(0ul, networkItem.RandomCircuitData);
        Assert.Equal(0u, networkItem.RandomGlyphData);
        Assert.Empty(networkItem.Glyphs);
    }

    [Fact]
    public void PopulateNetworkItem_PrefersPersistedMicrochipIdsWhenPresent()
    {
        var networkItem = new NetworkItem();
        ItemRuneNetworkWire.Populate(networkItem, [], [7u, 8u]);

        Assert.Equal([7u, 8u], networkItem.Microchips);
    }

    [Fact]
    public void RuneSlotsCodec_RoundTripsSlotTypesAndInstalledRuneItemIds()
    {
        var slots = new List<ItemRuneSlot>
        {
            new(RuneType.Air) { RuneItem2Id = 9001u },
            new(RuneType.Fusion),
            new(RuneType.Life) { RuneItem2Id = 9002u },
        };

        string encoded = ItemRuneSlotsCodec.Serialize(slots);
        var decoded = new List<ItemRuneSlot>();
        ItemRuneSlotsCodec.DeserializeInto(encoded, decoded);

        Assert.Equal("7:9001,13:0,12:9002", encoded);
        Assert.Collection(decoded,
            slot =>
            {
                Assert.Equal(RuneType.Air, slot.Type);
                Assert.Equal(9001u, slot.RuneItem2Id);
            },
            slot =>
            {
                Assert.Equal(RuneType.Fusion, slot.Type);
                Assert.Equal(0u, slot.RuneItem2Id);
            },
            slot =>
            {
                Assert.Equal(RuneType.Life, slot.Type);
                Assert.Equal(9002u, slot.RuneItem2Id);
            });
    }

    [Fact]
    public void ValidateInstallTargets_RejectsRuneCountBeyondSockets()
    {
        IGameTableManager tables = CreateInstallValidationTables();

        IItem item = CreateItemWithRuneInstance(2u);

        TradeskillResult result = ItemRuneInstallValidator.ValidateInstallTargets(tables, item, [1u, 2u]);

        Assert.Equal(TradeskillResult.InvalidSlot, result);
    }

    private static IGameTableManager CreateRunecraftingGlyphTables(uint runeItem2Id, uint runeItem2TypeId, uint item2CategoryId = ItemRuneGlyphTypes.Category176Id)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.Item), CreateGameTable(new Item2Entry
        {
            Id              = runeItem2Id,
            Item2TypeId     = runeItem2TypeId,
            Item2CategoryId = item2CategoryId,
        }));
        proxy.SetProperty(nameof(IGameTableManager.Item2Category), CreateGameTable(new Item2CategoryEntry
        {
            Id           = item2CategoryId,
            TradeSkillId = ItemRuneGlyphTypes.RunecraftingTradeSkillId,
        }));
        return tables;
    }

    private static IGameTableManager CreateEmptyItemTables()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.Item), CreateGameTable<Item2Entry>());
        proxy.SetProperty(nameof(IGameTableManager.Item2Category), CreateGameTable<Item2CategoryEntry>());
        return tables;
    }

    private static IItem CreateGearWithSocket(RuneType type)
    {
        return CreateGearWithSockets(type);
    }

    private static IItem CreateGearWithSockets(params RuneType[] types)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        var slots = new List<ItemRuneSlot>();
        foreach (RuneType type in types)
            slots.Add(new ItemRuneSlot(type));

        itemProxy.SetProperty(nameof(IItem.RuneSlots), slots);
        return item;
    }

    private static IItem CreateItemWithRuneInstance(uint instanceId, uint itemId = 0u)
    {
        IItem item = RecordingDispatchProxy<IItem>.Create(out RecordingDispatchProxy<IItem> itemProxy);
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out RecordingDispatchProxy<IItemInfo> infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            Id                 = itemId,
            ItemRuneInstanceId = instanceId
        });
        itemProxy.SetProperty(nameof(IItem.Id), itemId);
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot>());
        return item;
    }

    private static IGameTableManager CreateItemRuneInstanceTables()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.ItemRuneInstance), CreateGameTable(new ItemRuneInstanceEntry
        {
            Id                    = 2u,
            DefinedSocketCount    = 1u,
            DefinedSocketType00   = 7u,
        }, new ItemRuneInstanceEntry
        {
            Id                    = 139u,
            DefinedSocketCount    = 1u,
            DefinedSocketType00   = 12u,
        }));
        return tables;
    }

    private static IGameTableManager CreateInstallValidationTables(uint runeItem2Id = 0u, uint runeItem2TypeId = 0u)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.ItemRuneInstance), CreateGameTable(new ItemRuneInstanceEntry
        {
            Id                    = 2u,
            DefinedSocketCount    = 1u,
            DefinedSocketType00   = 7u,
        }));
        proxy.SetProperty(nameof(IGameTableManager.Item), runeItem2Id == 0u
            ? CreateGameTable<Item2Entry>()
            : CreateGameTable(new Item2Entry
            {
                Id              = runeItem2Id,
                Item2TypeId     = runeItem2TypeId,
                Item2CategoryId = ItemRuneGlyphTypes.Category176Id,
            }));
        proxy.SetProperty(nameof(IGameTableManager.Item2Category), runeItem2Id == 0u
            ? CreateGameTable<Item2CategoryEntry>()
            : CreateGameTable(new Item2CategoryEntry
            {
                Id           = ItemRuneGlyphTypes.Category176Id,
                TradeSkillId = ItemRuneGlyphTypes.RunecraftingTradeSkillId,
            }));
        return tables;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Public | BindingFlags.Instance);
        return idField == null ? 0u : (uint)idField.GetValue(entry)!;
    }

    private static void SetAutoProperty<T>(T target, string propertyName, object value)
    {
        PropertyInfo property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property?.SetValue(target, value);
    }

    private static void SetPrivateField<T>(T target, string fieldName, object value)
    {
        FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(target, value);
    }
}
