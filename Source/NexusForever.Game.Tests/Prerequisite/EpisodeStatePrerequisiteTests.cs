using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Prerequisite;

// Prerequisite type 9 EpisodeState (agent-written native read, Decomp/Analysis/PREREQUISITE_9_149_WRITERS_NATIVE_READ_20261108.md; the recompute FUN_140787d10 and the aggregate FUN_140788b50):
// the client keeps one node per quest category present among the episode's quests (category 0 is category 1). Each recompute resets the nodes, then for every quest with a Quest2 row reads the
// player's quest state (0 when none): state 0 clears the category's "done" flag unless the EpisodeQuest row is optional (Flags & 1); states 1, 2 and 4 clear it the same way and raise the node to 2;
// state 3 raises the node to 2 and keeps the flag; states 5, 6 and 7 clear it the same way and raise the node to 1. A category whose flag survives becomes 3. The episode state is 3 when every node is 3
// or there are no nodes, otherwise the highest node that is not 3. The prerequisite is true when that state equals Value (Equal) or differs (NotEqual).
public class EpisodeStatePrerequisiteTests
{
    private const uint EpisodeId = 7u;

    [Fact]
    public void AnEpisodeWhoseQuestsAreAllCompletedIsComplete()
    {
        var check = Check(Quests((10, 1, 0), (11, 1, 0)));

        Assert.True(Meets(check, Player((10, QuestState.Completed), (11, QuestState.Completed)), PrerequisiteComparison.Equal, 3u));
    }

    [Fact]
    public void AnUntouchedNonOptionalQuestKeepsTheEpisodeFromBeingComplete()
    {
        var check = Check(Quests((10, 1, 0), (11, 1, 0)));
        IPlayer player = Player((10, QuestState.Completed));

        Assert.False(Meets(check, player, PrerequisiteComparison.Equal, 3u));
        Assert.True(Meets(check, player, PrerequisiteComparison.Equal, 2u)); // the completed quest raised the category to 2
    }

    [Fact]
    public void AnEpisodeWithNothingStartedIsUnknown()
    {
        var check = Check(Quests((10, 1, 0), (11, 1, 0)));

        Assert.True(Meets(check, Player(), PrerequisiteComparison.Equal, 0u));
    }

    [Fact]
    public void AnUntouchedOptionalQuestDoesNotBlockCompletion()
    {
        var check = Check(Quests((10, 1, 0), (11, 1, 1)));

        Assert.True(Meets(check, Player((10, QuestState.Completed)), PrerequisiteComparison.Equal, 3u));
    }

    [Fact]
    public void AnActiveQuestMakesTheEpisodeActive()
    {
        var check = Check(Quests((10, 1, 0)));

        Assert.True(Meets(check, Player((10, QuestState.Accepted)), PrerequisiteComparison.Equal, 2u));
        Assert.False(Meets(check, Player((10, QuestState.Accepted)), PrerequisiteComparison.Equal, 3u));
    }

    [Theory]
    [InlineData(QuestState.Mentioned)]
    [InlineData(QuestState.Abandoned)]
    [InlineData(QuestState.Ignored)]
    public void AMentionedAbandonedOrIgnoredQuestMakesTheEpisodeMentioned(QuestState state)
    {
        var check = Check(Quests((10, 1, 0)));

        Assert.True(Meets(check, Player((10, state)), PrerequisiteComparison.Equal, 1u));
    }

    [Fact]
    public void ABotchedQuestCountsAsActiveButNotDone()
    {
        var check = Check(Quests((10, 1, 0)));

        Assert.True(Meets(check, Player((10, QuestState.Botched)), PrerequisiteComparison.Equal, 2u));
    }

    [Fact]
    public void TheHighestIncompleteCategoryDecidesTheEpisodeState()
    {
        // category 1 is complete (3), category 2 has an accepted quest (2) and one mentioned quest (1 -> stays 2)
        var check = Check(Quests((10, 1, 0), (20, 2, 0), (21, 2, 0)));
        IPlayer player = Player((10, QuestState.Completed), (20, QuestState.Accepted), (21, QuestState.Mentioned));

        Assert.True(Meets(check, player, PrerequisiteComparison.Equal, 2u));
    }

    [Fact]
    public void CategoryZeroSharesTheNodeOfCategoryOne()
    {
        // quests 10 (category 0) and 11 (category 1) share a node, so both must be completed for it to be 3
        var check = Check(Quests((10, 0, 0), (11, 1, 0)));

        Assert.False(Meets(check, Player((10, QuestState.Completed)), PrerequisiteComparison.Equal, 3u));
        Assert.True(Meets(check, Player((10, QuestState.Completed), (11, QuestState.Completed)), PrerequisiteComparison.Equal, 3u));
    }

    [Fact]
    public void ACategoryOfOnlyOptionalQuestsIsCompleteFromTheStart()
    {
        var check = Check(Quests((10, 1, 1)));

        Assert.True(Meets(check, Player(), PrerequisiteComparison.Equal, 3u));
    }

    [Fact]
    public void AnEpisodeWithNoResolvableQuestsIsComplete()
    {
        // quest 99 has no Quest2 row, so the episode has no nodes
        var check = Check(Quests((99, 1, 0)), quest2: [10]);

        Assert.True(Meets(check, Player(), PrerequisiteComparison.Equal, 3u));
    }

    [Fact]
    public void NotEqualIsTheNegationAndOtherComparisonsAreFalse()
    {
        var check = Check(Quests((10, 1, 0)));
        IPlayer player = Player((10, QuestState.Completed));

        Assert.False(Meets(check, player, PrerequisiteComparison.NotEqual, 3u)); // the single quest is completed, so the episode is 3
        Assert.True(Meets(check, player, PrerequisiteComparison.NotEqual, 2u));
        Assert.False(Meets(check, player, PrerequisiteComparison.GreaterThan, 1u));
    }

    [Fact]
    public void AnUnknownEpisodeHasNoState()
    {
        var check = Check(Quests((10, 1, 0)));
        var parameters = new PrerequisiteParameters();

        Assert.True(check.Meets(Player(), PrerequisiteComparison.Equal, 0u, EpisodeId + 1u, parameters));
        Assert.False(check.Meets(Player(), PrerequisiteComparison.Equal, 3u, EpisodeId + 1u, parameters));
    }

    private static bool Meets(PrerequisiteCheckEpisodeState check, IPlayer player, PrerequisiteComparison comparison, uint value)
        => check.Meets(player, comparison, value, EpisodeId, new PrerequisiteParameters());

    // (quest id, category id, EpisodeQuest flags)
    private static (uint Quest, uint Category, uint Flags)[] Quests(params (uint Quest, uint Category, uint Flags)[] quests) => quests;

    private static PrerequisiteCheckEpisodeState Check((uint Quest, uint Category, uint Flags)[] quests, uint[] quest2 = null)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.Episode), PlayerBindPointTests.Table(new EpisodeEntry { Id = EpisodeId }));
        proxy.SetProperty(nameof(IGameTableManager.EpisodeQuest), PlayerBindPointTests.Table(
            quests.Select((q, i) => new EpisodeQuestEntry { Id = (uint)i + 1u, EpisodeId = EpisodeId, QuestId = q.Quest, OrderIdx = (uint)i, Flags = q.Flags }).ToArray()));
        proxy.SetProperty(nameof(IGameTableManager.Quest2), PlayerBindPointTests.Table(
            quests.Where(q => quest2 == null || quest2.Contains(q.Quest))
                  .Select(q => new Quest2Entry { Id = q.Quest, QuestCategoryId = q.Category }).ToArray()));
        return new PrerequisiteCheckEpisodeState(tables);
    }

    private static IPlayer Player(params (ushort Quest, QuestState State)[] states)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Type), EntityType.Player);
        IQuestManager quests = RecordingDispatchProxy<IQuestManager>.Create(out var questProxy);
        questProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), args =>
            args[0] is ushort id && states.Any(s => s.Quest == id) ? states.First(s => s.Quest == id).State : (QuestState?)null);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), quests);
        return player;
    }
}
