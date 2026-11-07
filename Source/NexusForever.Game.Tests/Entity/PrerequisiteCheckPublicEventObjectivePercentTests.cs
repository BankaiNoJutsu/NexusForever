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

// Native evidence (Decomp/Analysis/PREREQUISITE_PUBLIC_EVENT_OBJECTIVE_PERCENT_20261107.md): prerequisite type 162 (FUN_1404a0a70) looks the public event objective ObjectId up in the public event manager and,
// when it exists and is active, takes count (virtual +0x98) divided by its required count (virtual +0xd0, 0 when that is 0) times 100.0 (another objective mode, virtual +0xf0, uses a float progress
// directly); a missing objective is 0. The value is compared with Value through the FLOAT helper FUN_1404a2010 (actual first). Retail: 8 slots, Values 100, 50, 30, 60, comparisons >= x4 and == x4.
// Status: Implemented, inferred (the +0xf0 float mode is not modelled; the server's required count is DynamicMax when set, otherwise the table Count).
public class PrerequisiteCheckPublicEventObjectivePercentTests
{
    private const ulong CharacterId = 77ul;

    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 50u, 4u, 8u, true)]     // 4 of 8 = 50%
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 51u, 4u, 8u, false)]
    [InlineData(PrerequisiteComparison.Equal, 50u, 4u, 8u, true)]
    [InlineData(PrerequisiteComparison.Equal, 100u, 8u, 8u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 100u, 12u, 8u, true)]          // over the maximum is above 100
    [InlineData(PrerequisiteComparison.LessThan, 30u, 2u, 10u, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 20u, 2u, 10u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 20u, 2u, 10u, false)]
    [InlineData(PrerequisiteComparison.Equal, 33u, 1u, 3u, false)]                  // 33.33... is not exactly 33 (float compare)
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 33u, 1u, 3u, true)]
    [InlineData((PrerequisiteComparison)0, 50u, 4u, 8u, false)]
    public void ComparesTheObjectivePercentageAsAFloat(PrerequisiteComparison comparison, uint value, uint count, uint max, bool expected)
    {
        IPlayer player = Player(objectiveId: 900, count: count, entryCount: max, dynamicMax: 0u);

        Assert.Equal(expected, new PrerequisiteCheckPublicEventObjective162().Meets(player, comparison, value, 900u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ADynamicMaximumReplacesTheTableCount()
    {
        IPlayer player = Player(objectiveId: 900, count: 3u, entryCount: 100u, dynamicMax: 6u);

        Assert.True(new PrerequisiteCheckPublicEventObjective162().Meets(player, PrerequisiteComparison.Equal, 50u, 900u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AMissingObjectiveOrAZeroMaximumIsZeroPercent()
    {
        IPlayer player = Player(objectiveId: 900, count: 3u, entryCount: 0u, dynamicMax: 0u);
        var check = new PrerequisiteCheckPublicEventObjective162();

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 900u, new PrerequisiteParameters()));        // zero maximum
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 901u, new PrerequisiteParameters()));        // no such objective
        Assert.False(check.Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 1u, 901u, new PrerequisiteParameters()));
    }

    private static IPlayer Player(uint objectiveId, uint count, uint entryCount, uint dynamicMax)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), CharacterId);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        IPublicEventManager manager = RecordingDispatchProxy<IPublicEventManager>.Create(out var managerProxy);
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out var eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Id), 40u);
        eventProxy.SetProperty(nameof(IPublicEvent.IsFinalised), false);

        IPublicEventTeam team = RecordingDispatchProxy<IPublicEventTeam>.Create(out var teamProxy);
        IPublicEventObjective objective = RecordingDispatchProxy<IPublicEventObjective>.Create(out var objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry { Id = objectiveId, Count = entryCount });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), PublicEventStatus.Active);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Count), count);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.DynamicMax), dynamicMax);
        teamProxy.SetMethodReturn(nameof(IPublicEventTeam.GetObjectives), new List<IPublicEventObjective> { objective });
        IPublicEventTeamMember member = RecordingDispatchProxy<IPublicEventTeamMember>.Create(out var memberProxy);
        memberProxy.SetProperty(nameof(IPublicEventTeamMember.CharacterId), CharacterId);
        teamProxy.SetMethodReturn(nameof(IPublicEventTeam.GetMembers), new List<IPublicEventTeamMember> { member });
        eventProxy.SetMethodReturn(nameof(IPublicEvent.GetTeams), new List<IPublicEventTeam> { team });

        managerProxy.SetMethodHandler(nameof(IPublicEventManager.GetEvent), args => (uint)args[0] == 40u ? publicEvent : null);
        managerProxy.SetMethodReturn(nameof(IPublicEventManager.GetEvents), new List<IPublicEvent> { publicEvent });
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), manager);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        return player;
    }
}
