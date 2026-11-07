using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

// Native evaluator (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md): type 120 (FUN_1404a0760) looks up the public event with id ObjectId, requires it to be active, and then
// either accepts any phase (Value 0) or requires the event's phase (virtual +0x170) to equal Value; Equal returns the result, NotEqual its negation, other comparisons are false.
// Retail rows: ObjectId is a PublicEvent id (41 of 42), comparison always Equal, Value 1..8. Type 123 (FUN_1404a09d0) reads the objective's count (virtual +0x98, the numerator of the
// type 162 percentage) and compares it with Value using the comparison; retail rows: ObjectId is a PublicEventObjective id in all 184 slots, comparisons mostly >= and =.
public class PrerequisiteCheckPublicEventPhaseCountTests
{
    private const ulong CharacterId = 77ul;

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 0u, true)]    // Value 0 accepts any phase while the event runs
    [InlineData(PrerequisiteComparison.Equal, 3u, true)]    // the event is in phase 3
    [InlineData(PrerequisiteComparison.Equal, 4u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 3u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 4u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 3u, false)]
    public void PublicEvent120_ComparesTheEventPhaseWhileTheEventRuns(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(eventId: 40, finalised: false, phase: 3u, objectiveId: 900, count: 0u);

        Assert.Equal(expected, new PrerequisiteCheckPublicEvent120().Meets(player, comparison, value, 40u, new PrerequisiteParameters()));
    }

    [Fact]
    public void PublicEvent120_ANotRunningOrMissingEventNeverMatchesEqual()
    {
        IPlayer finished = Player(eventId: 40, finalised: true, phase: 3u, objectiveId: 900, count: 0u);
        IPlayer running = Player(eventId: 40, finalised: false, phase: 3u, objectiveId: 900, count: 0u);

        Assert.False(new PrerequisiteCheckPublicEvent120().Meets(finished, PrerequisiteComparison.Equal, 0u, 40u, new PrerequisiteParameters()));
        Assert.True(new PrerequisiteCheckPublicEvent120().Meets(finished, PrerequisiteComparison.NotEqual, 3u, 40u, new PrerequisiteParameters()));
        Assert.False(new PrerequisiteCheckPublicEvent120().Meets(running, PrerequisiteComparison.Equal, 0u, 41u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 4u, true)]
    [InlineData(PrerequisiteComparison.Equal, 5u, false)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 4u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 5u, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, 3u, true)]
    [InlineData(PrerequisiteComparison.LessThan, 5u, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 3u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 4u, false)]
    public void PublicEventObjective123_ComparesTheObjectiveCountWithTheValue(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IPlayer player = Player(eventId: 40, finalised: false, phase: 1u, objectiveId: 900, count: 4u);

        Assert.Equal(expected, new PrerequisiteCheckPublicEventObjective123().Meets(player, comparison, value, 900u, new PrerequisiteParameters()));
    }

    [Fact]
    public void PublicEventObjective123_AnObjectiveThatIsNotFoundHasCountZero()
    {
        IPlayer player = Player(eventId: 40, finalised: false, phase: 1u, objectiveId: 900, count: 4u);

        Assert.True(new PrerequisiteCheckPublicEventObjective123().Meets(player, PrerequisiteComparison.Equal, 0u, 901u, new PrerequisiteParameters()));
        Assert.False(new PrerequisiteCheckPublicEventObjective123().Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 1u, 901u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(uint eventId, bool finalised, uint phase, uint objectiveId, uint count)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), CharacterId);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        IPublicEventManager manager = RecordingDispatchProxy<IPublicEventManager>.Create(out var managerProxy);
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out var eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Id), eventId);
        eventProxy.SetProperty(nameof(IPublicEvent.IsFinalised), finalised);
        eventProxy.SetProperty(nameof(IPublicEvent.Phase), phase);

        IPublicEventTeam team = RecordingDispatchProxy<IPublicEventTeam>.Create(out var teamProxy);
        IPublicEventObjective objective = RecordingDispatchProxy<IPublicEventObjective>.Create(out var objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry { Id = objectiveId });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), PublicEventStatus.Active);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Count), count);
        teamProxy.SetMethodReturn(nameof(IPublicEventTeam.GetObjectives), new List<IPublicEventObjective> { objective });
        IPublicEventTeamMember member = RecordingDispatchProxy<IPublicEventTeamMember>.Create(out var memberProxy);
        memberProxy.SetProperty(nameof(IPublicEventTeamMember.CharacterId), CharacterId);
        teamProxy.SetMethodReturn(nameof(IPublicEventTeam.GetMembers), new List<IPublicEventTeamMember> { member });
        eventProxy.SetMethodReturn(nameof(IPublicEvent.GetTeams), new List<IPublicEventTeam> { team });

        managerProxy.SetMethodHandler(nameof(IPublicEventManager.GetEvent), args => (uint)args[0] == eventId ? publicEvent : null);
        managerProxy.SetMethodReturn(nameof(IPublicEventManager.GetEvents), new List<IPublicEvent> { publicEvent });
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), manager);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        return player;
    }
}
