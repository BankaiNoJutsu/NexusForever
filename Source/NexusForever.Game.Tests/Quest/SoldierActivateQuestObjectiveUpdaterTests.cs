using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

/// <summary>
/// Quest objective types 27 and 28: <c>Data</c> is a PathSoldierActivate id (native lookup, matching counts, Creature2 descriptions naming the quest objective).
/// The producer rule (an activation of the row's creature) is inferred. The fixture rows are retail rows 217 and 218 of quest 4520 plus an invented mode 0 row. No retail parity claim.
/// </summary>
public class SoldierActivateQuestObjectiveUpdaterTests
{
    private const uint Cage = 13677u;

    [Theory]
    [InlineData(28u, 217u, true)]     // mode 1
    [InlineData(27u, 217u, true)]
    [InlineData(28u, 900u, false)]    // mode 0: a plain count
    [InlineData(28u, 159u, false)]    // objective 14973: no row
    [InlineData(18u, 217u, false)]    // another type whose Data happens to be a row id
    public void OnlyTypes27And28WithAnActivateModeOf1Or2AreChecklists(uint type, uint data, bool expected)
    {
        Assert.Equal(expected, SoldierActivateQuestObjectiveUpdater.IsChecklist(new QuestObjectiveEntry { Type = type, Data = data }, Tables()));
    }

    [Fact]
    public void ActivatingTheRowCreatureSetsTheBitOfItsChecklistIndex()
    {
        IPlayer player = Player(out RecordingDispatchProxy<IQuestManager> quests);

        SoldierActivateQuestObjectiveUpdater.OnActivate(player, Cage, [], 9, Tables());

        var updates = quests.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)).Select(i => ((QuestObjectiveType)i.Arguments[0], (uint)i.Arguments[1], (uint)i.Arguments[2])).ToArray();
        Assert.Equal([(QuestObjectiveType.Unknown27, 217u, 9u), (QuestObjectiveType.RescuePublicEventCreatures, 217u, 9u)], updates);
    }

    [Fact]
    public void AModeZeroRowCountsEveryActivationAndATargetGroupMatchesToo()
    {
        IPlayer player = Player(out RecordingDispatchProxy<IQuestManager> quests);

        SoldierActivateQuestObjectiveUpdater.OnActivate(player, 1u, [77u], 9, Tables());

        Assert.All(quests.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)), i => Assert.Equal((900u, 1u), ((uint)i.Arguments[1], (uint)i.Arguments[2])));
        Assert.Equal(2, quests.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)).Count());
    }

    [Fact]
    public void AnUnrelatedCreatureCreditsNothing()
    {
        IPlayer player = Player(out RecordingDispatchProxy<IQuestManager> quests);

        SoldierActivateQuestObjectiveUpdater.OnActivate(player, 4242u, [5u], 0, Tables());

        Assert.Empty(quests.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
    }

    // Quest 4520 objective 6181: six of fourteen cages, each cage once.
    [Fact]
    public void AChecklistObjectiveCompletesWhenEnoughDistinctCagesWereOpened()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        IQuestInfo questInfo = RecordingDispatchProxy<IQuestInfo>.Create(out RecordingDispatchProxy<IQuestInfo> questInfoProxy);
        questInfoProxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = 4520u });

        var entry = new QuestObjectiveEntry { Id = 6181u, Type = 28u, Data = 217u, Count = 6u, Flags = 131u };
        var objective = new QuestObjective(player, questInfo, new QuestObjectiveInfo(entry, SoldierActivateQuestObjectiveUpdater.IsChecklist(entry, Tables())), index: 2);

        foreach (uint idx in new uint[] { 3u, 3u, 3u, 0u, 13u, 7u, 8u })
            objective.ObjectiveUpdate(idx);

        Assert.False(objective.IsComplete());    // five distinct cages
        objective.ObjectiveUpdate(1u);
        Assert.True(objective.IsComplete());
        Assert.Equal((1u << 3) | 1u | (1u << 13) | (1u << 7) | (1u << 8) | (1u << 1), objective.Progress);
    }

    private static IPlayer Player(out RecordingDispatchProxy<IQuestManager> quests)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), RecordingDispatchProxy<IQuestManager>.Create(out quests));
        return player;
    }

    private static IGameTableManager Tables()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out RecordingDispatchProxy<IGameTableManager> proxy);
        proxy.SetProperty(nameof(IGameTableManager.PathSoldierActivate), CreateGameTable(
            new PathSoldierActivateEntry { Id = 217u, Creature2Id = Cage, Count = 6u, SoldierActivateModeEnum = 1u },
            new PathSoldierActivateEntry { Id = 218u, Creature2Id = 20263u, Count = 1u, SoldierActivateModeEnum = 1u },
            new PathSoldierActivateEntry { Id = 900u, TargetGroupId = 77u, Count = 4u, SoldierActivateModeEnum = 0u }));
        return tables;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        typeof(GameTable<T>).GetField($"<{nameof(GameTable<T>.Entries)}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, entries);
        FieldInfo idField = typeof(T).GetField("Id", BindingFlags.Instance | BindingFlags.Public)!;
        uint maxId = entries.Max(e => (uint)idField.GetValue(e)!);
        typeof(GameTable<T>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new GameTableHeader { MaxId = maxId + 1u });
        int[] lookup = Enumerable.Repeat(-1, (int)maxId + 1).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(uint)idField.GetValue(entries[i])!] = i;
        typeof(GameTable<T>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, lookup);
        return table;
    }
}
