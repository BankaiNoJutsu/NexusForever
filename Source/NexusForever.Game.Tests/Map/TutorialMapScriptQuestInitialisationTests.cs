using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Tutorial;

namespace NexusForever.Game.Tests.Map;

public class TutorialMapScriptQuestInitialisationTests
{
    [Fact]
    public void OnAddToMap_WhenStarterQuestsAreGranted_UsesLiveQuestDeltaOnly()
    {
        (TutorialMapScript script, IQuestManager questManager, RecordingDispatchProxy<IQuestManager> questManagerProxy) = CreateScript();
        IPlayer player = CreatePlayer(questManager, questManagerProxy, starterQuestStates: new Dictionary<ushort, QuestState?>());

        script.OnAddToMap(player);

        RecordingDispatchProxy<IQuestManager>.Invocation questAdd = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAdd)));
        Assert.Equal((uint)10513, ((IQuestInfo)questAdd.Arguments[0]).Entry.Id);
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.SendInitialPackets)));
    }

    [Fact]
    public void OnAddToMap_WhenDominionStarterQuestsAreMissing_GrantsDominionMovementQuestOnly()
    {
        (TutorialMapScript script, IQuestManager questManager, RecordingDispatchProxy<IQuestManager> questManagerProxy) = CreateScript();
        IPlayer player = CreatePlayer(
            questManager,
            questManagerProxy,
            starterQuestStates: new Dictionary<ushort, QuestState?>(),
            faction: Faction.Dominion);

        script.OnAddToMap(player);

        ushort[] grantedQuestIds = questManagerProxy
            .GetInvocations(nameof(IQuestManager.QuestAdd))
            .Select(i => (ushort)((IQuestInfo)i.Arguments[0]).Entry.Id)
            .Order()
            .ToArray();

        Assert.Equal(new ushort[] { 10521 }, grantedQuestIds);
        Assert.DoesNotContain((ushort)10513, grantedQuestIds);
        Assert.DoesNotContain((ushort)10527, grantedQuestIds);
        Assert.DoesNotContain((ushort)10532, grantedQuestIds);
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.SendInitialPackets)));
    }

    [Fact]
    public void OnAddToMap_WhenStarterQuestsAlreadyExist_DoesNotRefreshQuestSnapshot()
    {
        (TutorialMapScript script, IQuestManager questManager, RecordingDispatchProxy<IQuestManager> questManagerProxy) = CreateScript();
        IPlayer player = CreatePlayer(questManager, questManagerProxy, new Dictionary<ushort, QuestState?>
        {
            [10513] = QuestState.Accepted
        });

        script.OnAddToMap(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAdd)));
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.SendInitialPackets)));
    }

    [Theory]
    [InlineData(Faction.Exile, 10513, 10527)]
    [InlineData(Faction.Dominion, 10521, 10532)]
    public void OnAddToMap_WhenMovementQuestIsCompleteAndHoverboardQuestIsMissing_RecoversHoverboardQuest(
        Faction faction,
        ushort movementQuestId,
        ushort hoverboardQuestId)
    {
        (TutorialMapScript script, IQuestManager questManager, RecordingDispatchProxy<IQuestManager> questManagerProxy) = CreateScript();
        IPlayer player = CreatePlayer(
            questManager,
            questManagerProxy,
            new Dictionary<ushort, QuestState?>
            {
                [movementQuestId] = QuestState.Completed
            },
            faction);

        script.OnAddToMap(player);

        RecordingDispatchProxy<IQuestManager>.Invocation questAdd = Assert.Single(questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAdd)));
        Assert.Equal((uint)hoverboardQuestId, ((IQuestInfo)questAdd.Arguments[0]).Entry.Id);
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.SendInitialPackets)));
    }

    [Fact]
    public void OnAddToMap_WhenFollowUpQuestAlreadyExists_DoesNotRestartStarterQuests()
    {
        (TutorialMapScript script, IQuestManager questManager, RecordingDispatchProxy<IQuestManager> questManagerProxy) = CreateScript();
        IPlayer player = CreatePlayer(questManager, questManagerProxy, new Dictionary<ushort, QuestState?>
        {
            [10518] = QuestState.Accepted
        });

        script.OnAddToMap(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.QuestAdd)));
        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.SendInitialPackets)));
    }

    private static (TutorialMapScript Script, IQuestManager QuestManager, RecordingDispatchProxy<IQuestManager> QuestManagerProxy) CreateScript()
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out _);
        IEntityCacheManager entityCacheManager = RecordingDispatchProxy<IEntityCacheManager>.Create(out _);
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out _);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questManagerProxy);

        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateEmptyGameTable<WorldLocation2Entry>());
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetQuestInfo), args => CreateQuestInfo((ushort)args[0]));
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3460u });
        questManagerProxy.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), Array.Empty<IQuest>());

        var script = new TutorialMapScript(
            NullLogger<TutorialMapScript>.Instance,
            cinematicFactory,
            entityCacheManager,
            entityFactory,
            globalQuestManager,
            gameTableManager);

        typeof(TutorialMapScript)
            .GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(script, map);

        return (script, questManager, questManagerProxy);
    }

    private static IPlayer CreatePlayer(
        IQuestManager questManager,
        RecordingDispatchProxy<IQuestManager> questManagerProxy,
        IReadOnlyDictionary<ushort, QuestState?> starterQuestStates,
        Faction faction = Faction.Exile)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out _);

        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args => starterQuestStates.TryGetValue((ushort)args[0], out QuestState? state) ? state : null);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 99u);
        playerProxy.SetProperty(nameof(IPlayer.Faction1), faction);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.HitRadius), 0.5f);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);

        return player;
    }

    private static IQuestInfo CreateQuestInfo(ushort questId)
    {
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out RecordingDispatchProxy<IQuestInfo> questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = questId });
        return questInfo;
    }

    private static GameTable<T> CreateEmptyGameTable<T>() where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));

        typeof(GameTable<T>)
            .GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, Array.Empty<T>());
        typeof(GameTable<T>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = 1u });
        typeof(GameTable<T>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new[] { -1 });

        return table;
    }
}
