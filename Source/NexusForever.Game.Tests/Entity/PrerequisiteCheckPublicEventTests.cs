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

// Native evaluator (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md): type 117 forwards (comparison, objectId) to a public event manager query on the event id and
// returns it for Equal or its negation for NotEqual; type 118 forwards (comparison, value, objectId), reads the public event objective value and compares it for equality with Value
// (0 when the objective is not found); other comparisons are false. Retail 118 rows use Value 1 and 2 (about 97%), with PublicEventStatus 0..3 as the matching enum.
public class PrerequisiteCheckPublicEventTests
{
    private const ulong CharacterId = 77ul;

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, true, false)]
    public void PublicEvent117_ComparesWhetherTheEventIsRunning(PrerequisiteComparison comparison, bool running, bool expected)
    {
        IPlayer player = Player(eventId: 40, finalised: !running, objectiveId: 0, PublicEventStatus.Active, inTeam: true);

        Assert.Equal(expected, new PrerequisiteCheckPublicEvent117().Meets(player, comparison, 0u, 40u, new PrerequisiteParameters()));
    }

    [Fact]
    public void PublicEvent117_AnEventThatDoesNotExistIsNotRunning()
    {
        IPlayer player = Player(eventId: 40, finalised: false, objectiveId: 0, PublicEventStatus.Active, inTeam: true);

        Assert.False(new PrerequisiteCheckPublicEvent117().Meets(player, PrerequisiteComparison.Equal, 0u, 41u, new PrerequisiteParameters()));
        Assert.True(new PrerequisiteCheckPublicEvent117().Meets(player, PrerequisiteComparison.NotEqual, 0u, 41u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PublicEventStatus.Inactive, 0u)]
    [InlineData(PublicEventStatus.Active, 1u)]
    [InlineData(PublicEventStatus.Succeeded, 2u)]
    [InlineData(PublicEventStatus.Failed, 3u)]
    public void PublicEventObjective118_EqualMeetsWhenTheObjectiveStatusIsTheValue(PublicEventStatus status, uint value)
    {
        IPlayer player = Player(eventId: 40, finalised: false, objectiveId: 900, status, inTeam: true);
        var check = new PrerequisiteCheckPublicEventObjective118();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, value, 900u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.NotEqual, value, 900u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, (value + 1) % 4, 900u, new PrerequisiteParameters()));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, (value + 1) % 4, 900u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, value, 900u, new PrerequisiteParameters())); // only Equal and NotEqual are defined
    }

    [Fact]
    public void PublicEventObjective118_AnObjectiveThatIsNotFoundReadsAsInactive()
    {
        IPlayer player = Player(eventId: 40, finalised: false, objectiveId: 900, PublicEventStatus.Succeeded, inTeam: true);
        var check = new PrerequisiteCheckPublicEventObjective118();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 901u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 2u, 901u, new PrerequisiteParameters()));
    }

    [Fact]
    public void PublicEventObjective118_PrefersTheObjectiveOfTheTeamThePlayerIsOn()
    {
        // Two teams track the same objective id with different status; the player is a member of the second team only.
        IPlayer player = Player(eventId: 40, finalised: false, objectiveId: 900, PublicEventStatus.Succeeded, inTeam: true, otherTeamStatus: PublicEventStatus.Failed);
        var check = new PrerequisiteCheckPublicEventObjective118();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 2u, 900u, new PrerequisiteParameters()));
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 3u, 900u, new PrerequisiteParameters()));
    }

    [Fact]
    public void WithoutAMapOrPublicEventManagerEventsAreNotRunningAndObjectivesAreInactive()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.False(new PrerequisiteCheckPublicEvent117().Meets(player, PrerequisiteComparison.Equal, 0u, 40u, new PrerequisiteParameters()));
        Assert.True(new PrerequisiteCheckPublicEventObjective118().Meets(player, PrerequisiteComparison.Equal, 0u, 900u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(uint eventId, bool finalised, uint objectiveId, PublicEventStatus status, bool inTeam, PublicEventStatus? otherTeamStatus = null)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), CharacterId);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        IPublicEventManager manager = RecordingDispatchProxy<IPublicEventManager>.Create(out var managerProxy);
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out var eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Id), eventId);
        eventProxy.SetProperty(nameof(IPublicEvent.IsFinalised), finalised);

        var teams = new List<IPublicEventTeam>();
        if (otherTeamStatus is { } other)
            teams.Add(Team(objectiveId, other, withPlayer: false));
        teams.Add(Team(objectiveId, status, withPlayer: inTeam));
        eventProxy.SetMethodReturn(nameof(IPublicEvent.GetTeams), teams);

        managerProxy.SetMethodHandler(nameof(IPublicEventManager.GetEvent), args => (uint)args[0] == eventId ? publicEvent : null);
        managerProxy.SetMethodReturn(nameof(IPublicEventManager.GetEvents), new List<IPublicEvent> { publicEvent });
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), manager);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        return player;
    }

    private static IPublicEventTeam Team(uint objectiveId, PublicEventStatus status, bool withPlayer)
    {
        IPublicEventTeam team = RecordingDispatchProxy<IPublicEventTeam>.Create(out var teamProxy);
        IPublicEventObjective objective = RecordingDispatchProxy<IPublicEventObjective>.Create(out var objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry { Id = objectiveId });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), status);
        teamProxy.SetMethodReturn(nameof(IPublicEventTeam.GetObjectives), new List<IPublicEventObjective> { objective });
        IPublicEventTeamMember member = RecordingDispatchProxy<IPublicEventTeamMember>.Create(out var memberProxy);
        memberProxy.SetProperty(nameof(IPublicEventTeamMember.CharacterId), withPlayer ? CharacterId : CharacterId + 1ul);
        teamProxy.SetMethodReturn(nameof(IPublicEventTeam.GetMembers), new List<IPublicEventTeamMember> { member });
        return team;
    }
}
