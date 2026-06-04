using System.Reflection;
using System.Runtime.CompilerServices;
using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Search;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Story;
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
    private const uint ArrivedAtTowerObjective = 4987u;
    private const uint NorthernWildsWorld = 426u;
    private const uint ExoLabZone = 729u;
    private const uint LoftiteCrystalCreature = 11205u;
    private const uint LoftiteCrystalGuid = 1005u;
    private const uint LoftiteCrystalHealth = 55u;
    private const uint LoftiteVirtualItem = 206u;
    private const float LoftiteCrystalRange = 5f;

    [Fact]
    public void Q3486LoftiteCrystal_OnAddToMap_SetsBranchRange()
    {
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);

        script.OnAddToMap(map);

        RecordingDispatchProxy<ICreatureEntity>.Invocation range = Assert.Single(
            ownerProxy.GetInvocations(nameof(ICreatureEntity.SetInRangeCheck)));
        Assert.Equal(LoftiteCrystalRange, range.Arguments[0]);
    }

    [Fact]
    public void Q3486LoftiteCrystal_OnEnterRange_WhenQuestAccepted_CreditsArrivalFragmentAndDespawns()
    {
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer player = CreatePlayerWithQuestState(QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnEnterRange(player);

        IReadOnlyList<RecordingDispatchProxy<IQuestManager>.Invocation> objectiveUpdates =
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate));
        Assert.Equal(2, objectiveUpdates.Count);

        Assert.Equal(ArrivedAtTowerObjective, objectiveUpdates[0].Arguments[0]);
        Assert.Equal(1u, objectiveUpdates[0].Arguments[1]);

        Assert.Equal(QuestObjectiveType.VirtualCollect, objectiveUpdates[1].Arguments[0]);
        Assert.Equal(LoftiteVirtualItem, objectiveUpdates[1].Arguments[1]);
        Assert.Equal(1u, objectiveUpdates[1].Arguments[2]);

        RecordingDispatchProxy<ICreatureEntity>.Invocation despawn = Assert.Single(
            ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Equal(LoftiteCrystalHealth, despawn.Arguments[0]);
        Assert.Equal(DamageType.Physical, despawn.Arguments[1]);
        Assert.Null(despawn.Arguments[2]);
    }

    [Fact]
    public void Q3486LoftiteCrystal_OnEnterRange_WhenQuestMissing_DoesNotCreditOrDespawn()
    {
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        IPlayer player = CreatePlayerWithQuestState(null, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnEnterRange(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Empty(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    [Fact]
    public void Q3486LoftiteCrystal_Update_WhenAcceptedPlayerAlreadyInRange_CollectsCrystal()
    {
        IPlayer player = CreatePlayerWithQuestState(QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            inRangePlayers: [player]);

        script.Update(0.1d);

        IReadOnlyList<RecordingDispatchProxy<IQuestManager>.Invocation> objectiveUpdates =
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate));
        Assert.Equal(2, objectiveUpdates.Count);

        Assert.Equal(ArrivedAtTowerObjective, objectiveUpdates[0].Arguments[0]);
        Assert.Equal(1u, objectiveUpdates[0].Arguments[1]);

        Assert.Equal(QuestObjectiveType.VirtualCollect, objectiveUpdates[1].Arguments[0]);
        Assert.Equal(LoftiteVirtualItem, objectiveUpdates[1].Arguments[1]);
        Assert.Equal(1u, objectiveUpdates[1].Arguments[2]);

        RecordingDispatchProxy<ICreatureEntity>.Invocation despawn = Assert.Single(
            ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Equal(LoftiteCrystalHealth, despawn.Arguments[0]);
    }

    [Fact]
    public void Q3486LoftiteCrystal_Update_AfterCollected_DoesNotGrantAgain()
    {
        IPlayer player = CreatePlayerWithQuestState(QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
            inRangePlayers: [player]);

        script.Update(0.1d);
        script.Update(0.1d);

        Assert.Equal(2, questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)).Count);
        Assert.Single(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    [Fact]
    public void Q3486LoftiteCrystal_OnEnterRange_WhenEntityIsNotPlayer_DoesNothing()
    {
        Q3486LoftiteCrystalEntityScript script = CreateLoftiteCrystalScript(
            out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out _);

        script.OnEnterRange(creature);

        Assert.Empty(ownerProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    [Fact]
    public void Q3486CrystalGuardian_OnKilled_WhenQuestAccepted_CreditsArrivalObjective()
    {
        var script = new Q3486CrystalGuardianEntityScript();
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out _);
        IPlayer killer = CreatePlayerWithQuestState(QuestState.Accepted, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnLoad(owner);
        script.OnKilled(killer);

        RecordingDispatchProxy<IQuestManager>.Invocation arrival = Assert.Single(
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Equal(ArrivedAtTowerObjective, arrival.Arguments[0]);
        Assert.Equal(1u, arrival.Arguments[1]);
    }

    [Fact]
    public void Q3486CrystalGuardian_OnKilled_WhenQuestMissing_DoesNotCreditArrivalObjective()
    {
        var script = new Q3486CrystalGuardianEntityScript();
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out _);
        IPlayer killer = CreatePlayerWithQuestState(null, out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        script.OnLoad(owner);
        script.OnKilled(killer);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    [Fact]
    public void Q3486CrystalGuardian_OnKilled_WhenKillerIsNotPlayer_DoesNothing()
    {
        var script = new Q3486CrystalGuardianEntityScript();
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out _);
        ICreatureEntity killer = RecordingDispatchProxy<ICreatureEntity>.Create(out _);

        script.OnLoad(owner);
        script.OnKilled(killer);
    }

    [Fact]
    public void NorthernWildsMapScript_Update_WhenQ3486AcceptedAndPlayerOverlapsCrystalVolume_CollectsCrystal()
    {
        IPlayer player = CreatePlayerWithQuestState(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            position: Vector3.Zero,
            zoneId: ExoLabZone,
            hitRadius: 2f);
        ICreatureEntity crystal = CreateLoftiteCrystal(
            position: new Vector3(6f, 9f, 0f),
            hitRadius: 10f,
            out RecordingDispatchProxy<ICreatureEntity> crystalProxy);
        NorthernWildsMapScript script = CreateNorthernWildsMapScript([player], [crystal]);

        script.Update(0.1d);

        IReadOnlyList<RecordingDispatchProxy<IQuestManager>.Invocation> objectiveUpdates =
            questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate));
        Assert.Equal(2, objectiveUpdates.Count);

        Assert.Equal(ArrivedAtTowerObjective, objectiveUpdates[0].Arguments[0]);
        Assert.Equal(1u, objectiveUpdates[0].Arguments[1]);

        Assert.Equal(QuestObjectiveType.VirtualCollect, objectiveUpdates[1].Arguments[0]);
        Assert.Equal(LoftiteVirtualItem, objectiveUpdates[1].Arguments[1]);
        Assert.Equal(1u, objectiveUpdates[1].Arguments[2]);

        RecordingDispatchProxy<ICreatureEntity>.Invocation despawn = Assert.Single(
            crystalProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
        Assert.Equal(LoftiteCrystalHealth, despawn.Arguments[0]);
    }

    [Fact]
    public void NorthernWildsMapScript_Update_WhenQ3486Missing_DoesNotCollectCrystal()
    {
        IPlayer player = CreatePlayerWithQuestState(
            null,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            position: Vector3.Zero,
            zoneId: ExoLabZone,
            hitRadius: 2f);
        ICreatureEntity crystal = CreateLoftiteCrystal(
            position: new Vector3(6f, 9f, 0f),
            hitRadius: 10f,
            out RecordingDispatchProxy<ICreatureEntity> crystalProxy);
        NorthernWildsMapScript script = CreateNorthernWildsMapScript([player], [crystal]);

        script.Update(0.1d);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Empty(crystalProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    [Fact]
    public void NorthernWildsMapScript_Update_AfterCrystalCollected_DoesNotCollectAgain()
    {
        IPlayer player = CreatePlayerWithQuestState(
            QuestState.Accepted,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            position: Vector3.Zero,
            zoneId: ExoLabZone,
            hitRadius: 2f);
        ICreatureEntity crystal = CreateLoftiteCrystal(
            position: new Vector3(6f, 9f, 0f),
            hitRadius: 10f,
            out RecordingDispatchProxy<ICreatureEntity> crystalProxy);
        NorthernWildsMapScript script = CreateNorthernWildsMapScript([player], [crystal]);

        script.Update(0.1d);
        script.Update(0.1d);

        Assert.Equal(2, questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)).Count);
        Assert.Single(crystalProxy.GetInvocations(nameof(ICreatureEntity.ModifyHealth)));
    }

    private static Q3486LoftiteCrystalEntityScript CreateLoftiteCrystalScript(
        out RecordingDispatchProxy<ICreatureEntity> ownerProxy,
        IReadOnlyList<IPlayer> inRangePlayers = null)
    {
        ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out ownerProxy);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Guid), LoftiteCrystalGuid);
        ownerProxy.SetProperty(nameof(ICreatureEntity.Health), LoftiteCrystalHealth);
        if (inRangePlayers != null)
            ownerProxy.SetMethodHandler(nameof(ICreatureEntity.GetInRange), _ => inRangePlayers);

        var script = new Q3486LoftiteCrystalEntityScript();
        script.OnLoad(owner);
        return script;
    }

    private static ICreatureEntity CreateLoftiteCrystal(
        Vector3 position,
        float hitRadius,
        out RecordingDispatchProxy<ICreatureEntity> crystalProxy)
    {
        ICreatureEntity crystal = RecordingDispatchProxy<ICreatureEntity>.Create(out crystalProxy);
        crystalProxy.SetProperty(nameof(ICreatureEntity.CreatureId), LoftiteCrystalCreature);
        crystalProxy.SetProperty(nameof(ICreatureEntity.Guid), LoftiteCrystalGuid);
        crystalProxy.SetProperty(nameof(ICreatureEntity.Health), LoftiteCrystalHealth);
        crystalProxy.SetProperty(nameof(ICreatureEntity.Position), position);
        crystalProxy.SetProperty(nameof(ICreatureEntity.HitRadius), hitRadius);
        return crystal;
    }

    private static NorthernWildsMapScript CreateNorthernWildsMapScript(
        IReadOnlyList<IPlayer> players,
        IReadOnlyList<IWorldEntity> crystals)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = NorthernWildsWorld });
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), RecordingDispatchProxy<IPublicEventManager>.Create(out _));
        mapProxy.SetMethodHandler(nameof(IBaseMap.Search), args =>
        {
            if (args[1] == null)
                return players
                    .Where(player => ((ISearchCheck<IPlayer>)args[2]).CheckEntity(player))
                    .ToArray();

            return crystals
                .Where(crystal => ((ISearchCheck<IWorldEntity>)args[2]).CheckEntity(crystal))
                .ToArray();
        });

        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable<WorldLocation2Entry>());
        IStoryBuilder storyBuilder = RecordingDispatchProxy<IStoryBuilder>.Create(out _);

        var script = new NorthernWildsMapScript(
            NullLogger<NorthernWildsMapScript>.Instance,
            entityFactory,
            gameTableManager,
            cinematicFactory,
            storyBuilder);
        script.OnLoad(map);
        return script;
    }

    private static IPlayer CreatePlayerWithQuestState(
        QuestState? questState,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        Vector3? position = null,
        uint? zoneId = null,
        float hitRadius = 1f)
    {
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
            (ushort)args[0] == EmpoweredTowerQuest ? questState : null);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.Position), position ?? Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.HitRadius), hitRadius);
        if (zoneId.HasValue)
            playerProxy.SetProperty(nameof(IPlayer.Zone), new WorldZoneEntry { Id = zoneId.Value });

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
