using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Prerequisite;

// Native readings (Decomp/Analysis/PUBLIC_EVENT_NATIVE_READ_20261108.md, tails re-checked by decompiling the evaluators):
// 200 FUN_1404a0bf0: objective found and its event active, then (objective Busy or event Busy) & Value; Equal returns the masked word, NotEqual masked == 0, so a missing
//     objective or inactive event gives Equal false / NotEqual true. 122 FUN_1404a08f0: the per-unit map it consults is never filled, so Equal false / NotEqual true.
// 119 FUN_1404a0650: a Player/Ghost subject with no or a Player/Ghost target gives Equal false / NotEqual true; otherwise the unit is the target (for a player subject)
//     or the subject, its QuestChecklistIdx must be non-negative as a signed byte, and the result is the objective's ObjectiveData & (1 << idx), the parent event active.
public class PublicEventObjectiveStatePrerequisiteTests
{
    private const ulong CharacterId = 91ul;
    private const uint ObjectiveId = 700u;
    private static readonly IPrerequisiteParameters None = new PrerequisiteParameters();

    // ---- 122: dead in the client ----
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, false)]
    public void Objective122IsConstant(PrerequisiteComparison comparison, bool expected)
    {
        IPlayer player = Player(active: true, objectiveBusy: true, eventBusy: true, data: uint.MaxValue);

        Assert.Equal(expected, new PrerequisiteCheckPublicEventObjective122().Meets(player, comparison, 1u, ObjectiveId, None));
    }

    // ---- 200: busy flag masked with Value ----
    [Theory]
    [InlineData(true, false, 1u, true)]   // objective busy
    [InlineData(false, true, 1u, true)]   // event busy only
    [InlineData(false, false, 1u, false)] // neither
    [InlineData(true, true, 0u, false)]    // Value 0 masks everything (9 of the 11 retail slots)
    [InlineData(true, false, 2u, false)]  // busy is 1, so a Value without bit 0 never matches
    public void Objective200MasksTheBusyFlagWithValue(bool objectiveBusy, bool eventBusy, uint value, bool expected)
    {
        IPlayer player = Player(active: true, objectiveBusy, eventBusy, data: 0u);
        var check = new PrerequisiteCheckPublicEventObjective200();

        Assert.Equal(expected, check.Meets(player, PrerequisiteComparison.Equal, value, ObjectiveId, None));
        Assert.Equal(!expected, check.Meets(player, PrerequisiteComparison.NotEqual, value, ObjectiveId, None));
        Assert.False(check.Meets(player, PrerequisiteComparison.GreaterThan, value, ObjectiveId, None));
    }

    [Fact]
    public void Objective200WithAnInactiveEventOrUnknownObjectiveReadsAsZero()
    {
        var check = new PrerequisiteCheckPublicEventObjective200();

        IPlayer inactive = Player(active: false, objectiveBusy: true, eventBusy: true, data: 0u);
        Assert.False(check.Meets(inactive, PrerequisiteComparison.Equal, 1u, ObjectiveId, None));
        Assert.True(check.Meets(inactive, PrerequisiteComparison.NotEqual, 1u, ObjectiveId, None));

        IPlayer active = Player(active: true, objectiveBusy: true, eventBusy: true, data: 0u);
        Assert.False(check.Meets(active, PrerequisiteComparison.Equal, 1u, ObjectiveId + 1u, None));
        Assert.True(check.Meets(active, PrerequisiteComparison.NotEqual, 1u, ObjectiveId + 1u, None));
    }

    [Fact]
    public void Objective200RequiresThePlayerToBeInTheEvent()
    {
        IPlayer player = Player(active: true, objectiveBusy: true, eventBusy: false, data: 0u, member: false);

        Assert.False(new PrerequisiteCheckPublicEventObjective200().Meets(player, PrerequisiteComparison.Equal, 1u, ObjectiveId, None));
    }

    // ---- 119: checklist bit of the target creature ----
    [Theory]
    [InlineData((byte)0, 0b101u, true)]
    [InlineData((byte)1, 0b101u, false)]
    [InlineData((byte)2, 0b101u, true)]
    [InlineData((byte)32, 0b101u, true)]  // the index wraps at 32 (idx & 31)
    [InlineData((byte)255, 0b101u, false)] // -1: no checklist index
    [InlineData((byte)200, 0b101u, false)] // negative as a signed byte, treated as none
    public void Objective119TestsTheTargetCreaturesChecklistBit(byte idx, uint data, bool expected)
    {
        IPlayer player = Player(active: true, objectiveBusy: false, eventBusy: false, data);
        IUnitEntity target = Entity(EntityType.NonPlayer, idx);
        IPrerequisiteParameters parameters = new PrerequisiteParameters { Target = target };
        var check = new PrerequisiteCheckPublicEventObjectiveObject119();

        Assert.Equal(expected, check.Meets(player, PrerequisiteComparison.Equal, 0u, ObjectiveId, parameters));
        Assert.Equal(!expected, check.Meets(player, PrerequisiteComparison.NotEqual, 0u, ObjectiveId, parameters));
        Assert.False(check.Meets(player, PrerequisiteComparison.GreaterThan, 0u, ObjectiveId, parameters));
    }

    [Theory]
    [InlineData(EntityType.Player)]
    [InlineData(EntityType.Ghost)]
    public void Objective119WithAPlayerSubjectAndNoCreatureTargetIsConstant(EntityType targetType)
    {
        IPlayer player = Player(active: true, objectiveBusy: false, eventBusy: false, data: uint.MaxValue);
        var check = new PrerequisiteCheckPublicEventObjectiveObject119();

        foreach (IPrerequisiteParameters parameters in new[] { None, new PrerequisiteParameters { Target = Entity(targetType, 0) } })
        {
            Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, ObjectiveId, parameters));
            Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 0u, ObjectiveId, parameters));
        }
    }

    [Fact]
    public void Objective119WithACreatureSubjectUsesTheSubjectsOwnChecklistIndex()
    {
        IPlayer player = Player(active: true, objectiveBusy: false, eventBusy: false, data: 0b10u);
        IUnitEntity creature = Unit(EntityType.NonPlayer, 1, player);

        Assert.True(new PrerequisiteCheckPublicEventObjectiveObject119().MeetsUnit(creature, PrerequisiteComparison.Equal, 0u, ObjectiveId, None));
    }

    [Fact]
    public void Objective119WithAnInactiveEventIsZero()
    {
        IPlayer player = Player(active: false, objectiveBusy: false, eventBusy: false, data: uint.MaxValue);
        IPrerequisiteParameters parameters = new PrerequisiteParameters { Target = Entity(EntityType.NonPlayer, 0) };
        var check = new PrerequisiteCheckPublicEventObjectiveObject119();

        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, ObjectiveId, parameters));
        Assert.True(check.Meets(player, PrerequisiteComparison.NotEqual, 0u, ObjectiveId, parameters));
    }

    // ---- fixtures ----
    private static IUnitEntity Entity(EntityType type, byte checklistIdx)
    {
        IUnitEntity entity = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), type);
        proxy.SetProperty(nameof(IUnitEntity.QuestChecklistIdx), checklistIdx);
        return entity;
    }

    private static IUnitEntity Unit(EntityType type, byte checklistIdx, IPlayer mapSource)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), type);
        proxy.SetProperty(nameof(IUnitEntity.QuestChecklistIdx), checklistIdx);
        proxy.SetProperty(nameof(IUnitEntity.Map), mapSource.Map);
        return unit;
    }

    private static IPlayer Player(bool active, bool objectiveBusy, bool eventBusy, uint data, bool member = true)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), CharacterId);
        playerProxy.SetProperty(nameof(IPlayer.Type), EntityType.Player);

        IPublicEventObjective objective = RecordingDispatchProxy<IPublicEventObjective>.Create(out var objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry { Id = ObjectiveId });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.IsBusy), objectiveBusy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.ObjectiveData), data);

        IPublicEventTeamMember teamMember = RecordingDispatchProxy<IPublicEventTeamMember>.Create(out var memberProxy);
        memberProxy.SetProperty(nameof(IPublicEventTeamMember.CharacterId), member ? CharacterId : CharacterId + 1ul);
        IPublicEventTeam team = RecordingDispatchProxy<IPublicEventTeam>.Create(out var teamProxy);
        teamProxy.SetMethodReturn(nameof(IPublicEventTeam.GetObjectives), new List<IPublicEventObjective> { objective });
        teamProxy.SetMethodReturn(nameof(IPublicEventTeam.GetMembers), new List<IPublicEventTeamMember> { teamMember });

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out var eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Id), 40u);
        eventProxy.SetProperty(nameof(IPublicEvent.IsFinalised), !active);
        eventProxy.SetProperty(nameof(IPublicEvent.IsBusy), eventBusy);
        eventProxy.SetMethodReturn(nameof(IPublicEvent.GetTeams), new List<IPublicEventTeam> { team });

        IPublicEventManager manager = RecordingDispatchProxy<IPublicEventManager>.Create(out var managerProxy);
        managerProxy.SetMethodReturn(nameof(IPublicEventManager.GetEvents), new List<IPublicEvent> { publicEvent });
        managerProxy.SetMethodHandler(nameof(IPublicEventManager.GetEvent), args => (uint)args[0] == 40u ? publicEvent : null);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), manager);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        return player;
    }
}
