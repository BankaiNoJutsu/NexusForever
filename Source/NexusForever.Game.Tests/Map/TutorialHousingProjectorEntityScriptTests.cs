using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Tutorial;

namespace NexusForever.Game.Tests.Map;

public class TutorialHousingProjectorEntityScriptTests
{
    [Fact]
    public void OnActivateSuccess_WithClaimQuestAccepted_CreditsEnterZoneAndTeleports()
    {
        TutorialHousingProjectorEntityScript script = CreateScript(
            QuestState.Accepted,
            destination: new WorldLocation2Entry
            {
                Id        = 51711u,
                WorldId   = 3460u,
                Position0 = 10f,
                Position1 = 20f,
                Position2 = 30f
            },
            canTeleport: true,
            out IPlayer player,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);

        script.OnActivateSuccess(player);

        Assert.Contains(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)), invocation =>
            invocation.Arguments.Length == 3
            && (QuestObjectiveType)invocation.Arguments[0] == QuestObjectiveType.EnterZone
            && (uint)invocation.Arguments[1] == 4965u
            && (uint)invocation.Arguments[2] == 1u);

        RecordingDispatchProxy<IPlayer>.Invocation teleport = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
        Assert.Equal((ushort)3460, (ushort)teleport.Arguments[0]);
        Assert.Equal(10f, (float)teleport.Arguments[1]);
        Assert.Equal(20f, (float)teleport.Arguments[2]);
        Assert.Equal(30f, (float)teleport.Arguments[3]);
    }

    [Fact]
    public void OnActivateSuccess_WhenClaimQuestMissing_DoesNotCreditOrTeleport()
    {
        TutorialHousingProjectorEntityScript script = CreateScript(
            questState: null,
            destination: new WorldLocation2Entry
            {
                Id        = 51711u,
                WorldId   = 3460u,
                Position0 = 10f,
                Position1 = 20f,
                Position2 = 30f
            },
            canTeleport: true,
            out IPlayer player,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);

        script.OnActivateSuccess(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    [Fact]
    public void OnActivateSuccess_WhenTeleportUnavailable_DoesNotCreditObjective()
    {
        TutorialHousingProjectorEntityScript script = CreateScript(
            QuestState.Accepted,
            destination: new WorldLocation2Entry
            {
                Id        = 51711u,
                WorldId   = 3460u,
                Position0 = 10f,
                Position1 = 20f,
                Position2 = 30f
            },
            canTeleport: false,
            out IPlayer player,
            out RecordingDispatchProxy<IQuestManager> questManagerProxy,
            out RecordingDispatchProxy<IPlayer> playerProxy);

        script.OnActivateSuccess(player);

        Assert.Empty(questManagerProxy.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TeleportTo)));
    }

    private static TutorialHousingProjectorEntityScript CreateScript(
        QuestState? questState,
        WorldLocation2Entry destination,
        bool canTeleport,
        out IPlayer player,
        out RecordingDispatchProxy<IQuestManager> questManagerProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3460u });

        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out questManagerProxy);
        questManagerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
            (ushort)args[0] == 10525 ? questState : null);

        player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), canTeleport);

        ISimpleCollidableEntity owner = RecordingDispatchProxy<ISimpleCollidableEntity>.Create(out RecordingDispatchProxy<ISimpleCollidableEntity> ownerProxy);
        ownerProxy.SetProperty(nameof(IWorldEntity.Guid), 900u);
        ownerProxy.SetProperty(nameof(IWorldEntity.CreatureId), 73741u);
        ownerProxy.SetProperty(nameof(IWorldEntity.Map), map);

        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> gameTableManagerProxy);
        gameTableManagerProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(destination));

        var script = new TutorialHousingProjectorEntityScript(
            NullLogger<TutorialHousingProjectorEntityScript>.Instance,
            gameTableManager);
        script.OnLoad(owner);

        return script;
    }

    private static GameTable<WorldLocation2Entry> CreateGameTable(WorldLocation2Entry entry)
    {
        var table = (GameTable<WorldLocation2Entry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<WorldLocation2Entry>));

        typeof(GameTable<WorldLocation2Entry>)
            .GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new[] { entry });
        typeof(GameTable<WorldLocation2Entry>)
            .GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, new GameTableHeader { MaxId = entry.Id + 1u });

        int[] lookup = Enumerable.Repeat(-1, (int)entry.Id + 1).ToArray();
        lookup[entry.Id] = 0;
        typeof(GameTable<WorldLocation2Entry>)
            .GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(table, lookup);

        return table;
    }
}
