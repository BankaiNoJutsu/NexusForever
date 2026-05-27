using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Quests.NorthernWilds;

namespace NexusForever.Game.Tests.Quests;

public class NorthernWildsBranchInteractionTests
{
    private const ushort EmpoweredTowerQuest = 3486;
    private const uint LoftiteCrystalGuid = 1005u;
    private const uint LoftiteCrystalHealth = 55u;
    private const uint LoftiteVirtualItem = 206u;

    [Fact]
    public void Q3486LoftiteCrystal_OnAddToMap_SetsBranchRange()
    {
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out _);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);

        script.OnAddToMap(map);

        RecordingDispatchProxy<ICreatureEntity>.Invocation range = Assert.Single(
            ownerProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Equal(5f, range.Arguments[0]);
    }

    [Fact]
    public void Q3486LoftiteCrystal_OnEnterRange_WhenQuestAccepted_GivesVirtualItemAndDespawns()
    {
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IGlobalLootManager> lootManagerProxy);
        IPlayer player = CreatePlayerWithQuestState(QuestState.Accepted);

        script.OnEnterRange(player);

        RecordingDispatchProxy<IGlobalLootManager>.Invocation loot = Assert.Single(
            lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.GiveLoot)));
        Assert.Same(player, loot.Arguments[0]);
        VirtualItemEntry reward = Assert.IsType<VirtualItemEntry>(loot.Arguments[1]);
        Assert.Equal(LoftiteVirtualItem, reward.Id);
        Assert.Equal(1u, loot.Arguments[2]);
        Assert.Equal(LoftiteCrystalGuid, loot.Arguments[3]);

        RecordingDispatchProxy<ICreatureEntity>.Invocation despawn = Assert.Single(
            ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Equal(LoftiteCrystalHealth, despawn.Arguments[0]);
        Assert.Equal(DamageType.Physical, despawn.Arguments[1]);
        Assert.Null(despawn.Arguments[2]);
    }

    [Fact]
    public void Q3486LoftiteCrystal_OnEnterRange_WhenQuestMissing_DoesNotGiveLootOrDespawn()
    {
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IGlobalLootManager> lootManagerProxy);
        IPlayer player = CreatePlayerWithQuestState(null);

        script.OnEnterRange(player);

        Assert.Empty(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.GiveLoot)));
        Assert.Empty(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    [Fact]
    public void Q3486LoftiteCrystal_OnEnterRange_WhenEntityIsNotPlayer_DoesNothing()
    {
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            out RecordingDispatchProxy<IGlobalLootManager> lootManagerProxy);
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out _);

        script.OnEnterRange(creature);

        Assert.Empty(lootManagerProxy.GetInvocations(nameof(IGlobalLootManager.GiveLoot)));
        Assert.Empty(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    private static Q3486LoftiteCrystalEntityScript CreateLoftiteCrystalScript(
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
        out RecordingDispatchProxy<IGlobalLootManager> lootManagerProxy)
    {
        IGlobalLootManager lootManager = RecordingDispatchProxy<IGlobalLootManager>.Create(out lootManagerProxy);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(
            nameof(IGameTableManager.VirtualItem),
            CreateGameTable(new VirtualItemEntry { Id = LoftiteVirtualItem }));

        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out ownerProxy);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Guid), LoftiteCrystalGuid);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Health), LoftiteCrystalHealth);

        var script = new Q3486LoftiteCrystalEntityScript(lootManager, gameTableManager);
        script.OnLoad(owner);
        return script;
    }

    private static IPlayer CreatePlayerWithQuestState(QuestState? questState)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
            (ushort)args[0] == EmpoweredTowerQuest ? questState : null);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        return player;
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
        FieldInfo idField = typeof(T).GetField("Id")!;
        return (uint)idField.GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
