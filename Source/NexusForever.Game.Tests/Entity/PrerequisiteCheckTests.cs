using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Challenges;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Crafting;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PlayerPath;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Static;

namespace NexusForever.Game.Tests.Entity;

public class PrerequisiteCheckTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    public void UnderSpell_UsesActiveTrackedSpellState(PrerequisiteComparison comparison, bool active, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.HasTrackedSpellState), active);

        var check = new PrerequisiteCheckUnderSpell(NullLogger<PrerequisiteCheckUnderSpell>.Instance);

        bool result = check.Meets(player, comparison, value: 85563u, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 517u, true)]
    [InlineData(PrerequisiteComparison.Equal, 518u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 517u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 518u, true)]
    public void InSubZone_ComparesAgainstCurrentZone(PrerequisiteComparison comparison, uint requiredZoneId, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Zone), new WorldZoneEntry { Id = 517u });

        var check = new PrerequisiteCheckInSubZone(NullLogger<PrerequisiteCheckInSubZone>.Instance, gameTableManager: null);

        bool result = check.Meets(player, comparison, value: requiredZoneId, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 2u, true)]
    [InlineData(PrerequisiteComparison.Equal, 1u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 2u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 1u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 2u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 2u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 2u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 2u, false)]
    public void ChallengeRequirement_ComparesChallengeCompletionCount(PrerequisiteComparison comparison, uint requiredCount, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IChallengeManager challengeManager = RecordingDispatchProxy<IChallengeManager>.Create(out var challengeProxy);
        challengeProxy.SetMethodReturn(nameof(IChallengeManager.GetCompletionCount), 2u);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), challengeManager);

        var check = new PrerequisiteCheckChallengeRequirement(NullLogger<PrerequisiteCheckChallengeRequirement>.Instance);

        bool result = check.Meets(player, comparison, requiredCount, objectId: 971u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, SettlerInfrastructureState.Built, (uint)SettlerInfrastructureState.Built, true)]
    [InlineData(PrerequisiteComparison.Equal, SettlerInfrastructureState.Inactive, (uint)SettlerInfrastructureState.Built, false)]
    [InlineData(PrerequisiteComparison.NotEqual, SettlerInfrastructureState.Building, (uint)SettlerInfrastructureState.Built, true)]
    [InlineData(PrerequisiteComparison.NotEqual, SettlerInfrastructureState.Built, (uint)SettlerInfrastructureState.Built, false)]
    public void InfrastructureState_ComparesPathManagerState(
        PrerequisiteComparison comparison,
        SettlerInfrastructureState currentState,
        uint requiredState,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        pathProxy.SetMethodReturn(nameof(IPathManager.GetSettlerInfrastructureState), currentState);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);

        var check = new PrerequisiteCheckInfrastructureState(NullLogger<PrerequisiteCheckInfrastructureState>.Instance);

        bool result = check.Meets(player, comparison, requiredState, objectId: 2u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    public void PetEntitySpell4_UsesActiveVanityPetPresence(PrerequisiteComparison comparison, bool hasVanityPet, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        uint? vanityGuid = hasVanityPet ? 9001u : null;
        playerProxy.SetProperty(nameof(IPlayer.VanityPetGuid), vanityGuid);

        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out var spellManagerProxy);
        spellManagerProxy.SetMethodReturn(
            nameof(ISpellManager.GetPets),
            hasVanityPet
                ? new List<ICharacterSpell> { RecordingDispatchProxy<ICharacterSpell>.Create(out _) }
                : new List<ICharacterSpell>());
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        if (hasVanityPet)
        {
            IPetEntity pet = RecordingDispatchProxy<IPetEntity>.Create(out _);
            playerProxy.SetMethodReturn(nameof(IPlayer.GetVisible), pet);
        }
        else
        {
            playerProxy.SetMethodReturn(nameof(IPlayer.GetVisible), null);
        }

        var check = new PrerequisiteCheckPetEntitySpell4(NullLogger<PrerequisiteCheckPetEntitySpell4>.Instance);

        bool result = check.Meets(player, comparison, value: 0u, objectId: 0u, new PrerequisiteParameters());

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ItemRolledPropertyValue_WithoutItemContext_ReturnsFalse()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var check = new PrerequisiteCheckItemRolledPropertyValue();

        bool result = check.Meets(
            player,
            PrerequisiteComparison.Equal,
            value: 0u,
            objectId: (uint)Property.ShieldCapacityMax,
            new PrerequisiteParameters());

        Assert.False(result);
    }

    [Fact]
    public void ItemRolledPropertyValue_MatchesTemplatePropertyMagnitudeBits()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        float magnitude = 42f;
        itemInfoProxy.SetProperty(
            nameof(IItemInfo.Properties),
            ImmutableDictionary<Property, float>.Empty.Add(Property.ShieldCapacityMax, magnitude));

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var parameters = new PrerequisiteParameters { Item = item };
        var check = new PrerequisiteCheckItemRolledPropertyValue();
        uint magnitudeBits = unchecked((uint)BitConverter.SingleToInt32Bits(magnitude));

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, magnitudeBits, (uint)Property.ShieldCapacityMax, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.NotEqual, magnitudeBits, (uint)Property.ShieldCapacityMax, parameters));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 41f, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 42f, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 42f, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 42f, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 43f, true)]
    [InlineData(PrerequisiteComparison.LessThan, 42f, false)]
    public void ItemRolledPropertyValue_UsesInclusiveComparisonEdges(
        PrerequisiteComparison comparison,
        float expectedMagnitude,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(
            nameof(IItemInfo.Properties),
            ImmutableDictionary<Property, float>.Empty.Add(Property.ShieldCapacityMax, 42f));

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var parameters = new PrerequisiteParameters { Item = item };
        var check = new PrerequisiteCheckItemRolledPropertyValue();
        uint expectedBits = unchecked((uint)BitConverter.SingleToInt32Bits(expectedMagnitude));

        bool result = check.Meets(player, comparison, expectedBits, (uint)Property.ShieldCapacityMax, parameters);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 0u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 1u, false)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 0u, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 1u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 1u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 0u, false)]
    public void ItemRolledPropertyValue_MissingSlotComparesAsZero(
        PrerequisiteComparison comparison,
        uint expectedBits,
        bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Properties), ImmutableDictionary<Property, float>.Empty);

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var parameters = new PrerequisiteParameters { Item = item };
        var check = new PrerequisiteCheckItemRolledPropertyValue();

        bool result = check.Meets(player, comparison, expectedBits, (uint)Property.ShieldCapacityMax, parameters);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, false)]
    [InlineData(PrerequisiteComparison.Equal, 7u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 0u, true)]
    public void ItemSpecial_UsesTemplateSpecialOrMicrochips(PrerequisiteComparison comparison, uint itemSpecialId, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        var entry = new Item2Entry { ItemSpecialId00 = itemSpecialId };
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), entry);

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint>());
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot>());

        var check = new PrerequisiteCheckItemSpecial();
        bool result = check.Meets(player, comparison, 0u, 0u, new PrerequisiteParameters { Item = item });

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ItemSpecial_TreatsRuneSlotsAsSpecialPresence()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { ItemSpecialId00 = 0 });

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint>());
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot> { new(RuneType.Water) });

        var check = new PrerequisiteCheckItemSpecial();
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters { Item = item }));
    }

    [Fact]
    public void ItemSpecial_UsesItemRuneInstanceTemplateWhenSlotsEmpty()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry
        {
            ItemSpecialId00    = 0,
            ItemRuneInstanceId = 2u,
        });
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint>());
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot>());

        IGameTableManager tables = CreateItemRuneInstanceTablesForPrereq();
        var check = new PrerequisiteCheckItemSpecial(tables);

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters { Item = item }));
    }

    [Fact]
    public void ItemMicrochip_ObjectIdZero_ComparesInstalledCount()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint> { 1, 2 });

        var check = new PrerequisiteCheckItemMicrochip();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 2u, 0u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 1u, 0u, parameters));
    }

    [Fact]
    public void ItemMicrochip_SocketType_UsesRuneSlotTypeBitmask()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint>());
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot>
        {
            new(RuneType.Fire, 0u)
        });

        var check = new PrerequisiteCheckItemMicrochip();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, (uint)RuneType.Fire, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, (uint)RuneType.Air, parameters));
    }

    [Fact]
    public void ItemMicrochip_SocketType_FallsBackToItemRuneInstanceTemplate()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        IItemInfo info = RecordingDispatchProxy<IItemInfo>.Create(out var infoProxy);
        infoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { ItemRuneInstanceId = 2u });
        itemProxy.SetProperty(nameof(IItem.Info), info);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint>());
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot>());

        IGameTableManager tables = CreateItemRuneInstanceTablesForPrereq();
        var check = new PrerequisiteCheckItemMicrochip(tables);
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, (uint)RuneType.Air, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, (uint)RuneType.Water, parameters));
    }

    [Fact]
    public void ItemMicrochip_ObjectIdZero_PrefersInstalledRuneCountWhenNoWireMicrochips()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.MicrochipIds), new List<uint>());
        itemProxy.SetProperty(nameof(IItem.RuneSlots), new List<ItemRuneSlot>
        {
            new(RuneType.Fire, 100u),
            new(RuneType.Water, 0u)
        });

        var check = new PrerequisiteCheckItemMicrochip();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 0u, parameters));
    }

    [Fact]
    public void Item2Id_ComparesEvaluatedItemTemplateId()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Id), 12345u);

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var check = new PrerequisiteCheckItem2Id();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 12345u, parameters));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 0u, 999u, parameters));
    }

    [Fact]
    public void ItemLevel_ComparesRequiredLevel()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { RequiredLevel = 50 });

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var check = new PrerequisiteCheckItemLevel();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 50u, 0u, parameters));
        Assert.True(check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 40u, 0u, parameters));
    }

    [Fact]
    public void ItemStatId_ComparesTemplateItemStatId()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.Entry), new Item2Entry { ItemStatId = 42u });

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var check = new PrerequisiteCheckItemStatId();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 42u, 0u, parameters));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 99u, 0u, parameters));
    }

    [Fact]
    public void AppliedItemStatId_ComparesResolvedStatEntryId()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.StatEntry), new ItemStatEntry { Id = 77u });

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var check = new PrerequisiteCheckAppliedItemStatId();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 77u, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, 78u, parameters));
    }

    [Fact]
    public void ItemStatData_ComparesFirstStandardStatDataSlot()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItemInfo itemInfo = RecordingDispatchProxy<IItemInfo>.Create(out var itemInfoProxy);
        itemInfoProxy.SetProperty(nameof(IItemInfo.StatEntry), new ItemStatEntry
        {
            ItemStatTypeEnum = [ItemStatType.Standard, ItemStatType.None],
            ItemStatData     = [9001u, 0u]
        });

        IItem item = RecordingDispatchProxy<IItem>.Create(out var itemProxy);
        itemProxy.SetProperty(nameof(IItem.Info), itemInfo);

        var check = new PrerequisiteCheckItemStatData();
        var parameters = new PrerequisiteParameters { Item = item };

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 9001u, 0u, parameters));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 1u, 0u, parameters));
    }

    private static IGameTableManager CreateItemRuneInstanceTablesForPrereq()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.ItemRuneInstance), CreateGameTable(new ItemRuneInstanceEntry
        {
            Id                    = 2u,
            DefinedSocketCount    = 1u,
            DefinedSocketType00   = 7u,
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
