using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_QUEST_STATE_20261107.md): type 6 (FUN_14049f690) reads the player's state of quest ObjectId (FUN_1405fbc40), maps the states 4 to 7
// (Botched, Mentioned, Abandoned, Ignored) to 0, and compares the result for equality with Value: Equal returns that, NotEqual its negation, any other comparison is false. A quest the player has no record
// of has state 0. Retail: 8,790 slots, Equal 6,360, NotEqual 2,430, Value 3 (3,619), 2 (2,172), 1 (1,809), 0 (1,151) and 4 (39).
public class PrerequisiteCheckQuestStateTests
{
    public static TheoryData<QuestState?, uint, bool> States()
    {
        var data = new TheoryData<QuestState?, uint, bool>();
        foreach (QuestState? state in new QuestState?[]
                 { null, QuestState.Unknown, QuestState.Accepted, QuestState.Achieved, QuestState.Completed, QuestState.Botched, QuestState.Mentioned, QuestState.Abandoned, QuestState.Ignored })
        {
            uint mapped = state switch
            {
                null or QuestState.Botched or QuestState.Mentioned or QuestState.Abandoned or QuestState.Ignored => 0u,
                _ => (uint)state.Value
            };

            for (uint value = 0u; value <= 4u; value++)
                data.Add(state, value, mapped == value);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(States))]
    public void EqualComparesTheMappedStateAndNotEqualNegatesIt(QuestState? state, uint value, bool equal)
    {
        IPlayer player = Player(state);
        var check = new PrerequisiteCheckQuestState(NullLogger<PrerequisiteCheckQuestState>.Instance);

        Assert.Equal(equal, check.Meets(player, PrerequisiteComparison.Equal, value, 4944u, new PrerequisiteParameters()));
        Assert.Equal(!equal, check.Meets(player, PrerequisiteComparison.NotEqual, value, 4944u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThan)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual)]
    [InlineData(PrerequisiteComparison.LessThan)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual)]
    [InlineData((PrerequisiteComparison)0)]
    public void OtherComparisonsNeverMeet(PrerequisiteComparison comparison)
    {
        var check = new PrerequisiteCheckQuestState(NullLogger<PrerequisiteCheckQuestState>.Instance);

        Assert.False(check.Meets(Player(QuestState.Completed), comparison, 3u, 4944u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AQuestIdBeyondSixteenBitsHasNoRecord()
    {
        var check = new PrerequisiteCheckQuestState(NullLogger<PrerequisiteCheckQuestState>.Instance);
        IPlayer player = Player(QuestState.Completed);

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 100_000u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 3u, 100_000u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(QuestState? state)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IQuestManager manager = RecordingDispatchProxy<IQuestManager>.Create(out var managerProxy);
        managerProxy.SetMethodHandler(nameof(IQuestManager.GetQuestState), _ => state);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), manager);
        return player;
    }
}
