using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

// Native evaluator (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md): type 44 (FUN_14049ee40) takes the TargetGroup id from Value (214 of 214 retail slots; ObjectId is 0),
// evaluates the group against the unit with FUN_1403b4a20 (0 means member, 0x59 not) and returns Equal: member, NotEqual: not member, others false; a zero group id is false.
// Group type 1 lists creature ids the unit's creature must be in, 2 inverts it, 10 requires every listed sub-group and 11 any one of them. Retail slots: group type 1 (146),
// 11 (48), 3 (16), 12 (3), 10 (1); types 3..9, 12 and 13 test unit fields this server does not model and never meet.
public class PrerequisiteCheckInTargetGroupTests
{
    private const uint Creature = 5150;

    private static readonly TargetGroupEntry[] Groups =
    [
        Group(1, 1, Creature, 77),            // creature list
        Group(2, 1, 88, 99),                  // a list not containing the creature
        Group(3, 2, 88),                      // excluded list: member when the creature is not 88
        Group(4, 2, Creature),                // excluded list: not a member when the creature is listed
        Group(5, 11, 2, 1),                   // any of: group 2 (no), group 1 (yes)
        Group(6, 11, 2, 4),                   // any of: both fail
        Group(7, 10, 1, 3),                   // all of: both pass
        Group(8, 10, 1, 2),                   // all of: group 2 fails
        Group(9, 3, 1),                       // a type whose unit field is not modelled
        Group(10, 11, 10, 1),                 // refers to itself and to a passing group
        Group(11, 10, 9, 1)                   // all of: an unmodelled type and a passing group
    ];

    [Theory]
    [InlineData(1u, PrerequisiteComparison.Equal, true)]
    [InlineData(1u, PrerequisiteComparison.NotEqual, false)]
    [InlineData(2u, PrerequisiteComparison.Equal, false)]
    [InlineData(2u, PrerequisiteComparison.NotEqual, true)]
    [InlineData(3u, PrerequisiteComparison.Equal, true)]
    [InlineData(4u, PrerequisiteComparison.Equal, false)]
    [InlineData(4u, PrerequisiteComparison.NotEqual, true)]
    [InlineData(5u, PrerequisiteComparison.Equal, true)]
    [InlineData(6u, PrerequisiteComparison.Equal, false)]
    [InlineData(6u, PrerequisiteComparison.NotEqual, true)]
    [InlineData(7u, PrerequisiteComparison.Equal, true)]
    [InlineData(8u, PrerequisiteComparison.Equal, false)]
    [InlineData(8u, PrerequisiteComparison.NotEqual, true)]
    [InlineData(10u, PrerequisiteComparison.Equal, true)]           // the self reference is skipped, as the native loop skips the group's own id
    [InlineData(1u, PrerequisiteComparison.GreaterThan, false)]
    [InlineData(1u, PrerequisiteComparison.LessThan, false)]
    public void ComparesTheUnitsGroupMembership(uint groupId, PrerequisiteComparison comparison, bool expected)
    {
        IUnitEntity unit = Unit(Creature);

        Assert.Equal(expected, Check().MeetsUnit(unit, comparison, groupId, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal)]
    [InlineData(PrerequisiteComparison.NotEqual)]
    public void AGroupTypeThatTestsAnUnmodelledUnitFieldMeetsNeitherComparison(PrerequisiteComparison comparison)
    {
        Assert.False(Check().MeetsUnit(Unit(Creature), comparison, 9u, 0u, new PrerequisiteParameters()));
        Assert.False(Check().MeetsUnit(Unit(Creature), comparison, 11u, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AZeroOrUnknownGroupIdFailsEqualButAnUnknownGroupSatisfiesNotEqual()
    {
        IUnitEntity unit = Unit(Creature);

        Assert.False(Check().MeetsUnit(unit, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));
        Assert.False(Check().MeetsUnit(unit, PrerequisiteComparison.NotEqual, 0u, 0u, new PrerequisiteParameters()));
        Assert.False(Check().MeetsUnit(unit, PrerequisiteComparison.Equal, 999u, 0u, new PrerequisiteParameters()));
        Assert.True(Check().MeetsUnit(unit, PrerequisiteComparison.NotEqual, 999u, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AMissingTableMeetsNeitherComparison()
    {
        var check = new PrerequisiteCheckInTargetGroup(RecordingDispatchProxy<IGameTableManager>.Create(out _));

        Assert.False(check.MeetsUnit(Unit(Creature), PrerequisiteComparison.Equal, 1u, 0u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(Unit(Creature), PrerequisiteComparison.NotEqual, 1u, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ThePlayerEntryPointEvaluatesTheTargetWhenOneIsSupplied()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IWorldEntity.CreatureId), 12345u);

        var parameters = new PrerequisiteParameters { Target = Unit(Creature) };

        Assert.True(Check().Meets(player, PrerequisiteComparison.Equal, 1u, 0u, parameters));
        Assert.False(Check().Meets(player, PrerequisiteComparison.Equal, 1u, 0u, new PrerequisiteParameters()));
    }

    private static PrerequisiteCheckInTargetGroup Check()
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
        proxy.SetProperty(nameof(IGameTableManager.TargetGroup), PlayerBindPointTests.Table(Groups));
        return new PrerequisiteCheckInTargetGroup(tables);
    }

    private static TargetGroupEntry Group(uint id, uint type, params uint[] data)
    {
        uint[] entries = new uint[7];
        data.CopyTo(entries, 0);
        return new TargetGroupEntry { Id = id, Type = type, DataEntries = entries };
    }

    private static IUnitEntity Unit(uint creatureId)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        return unit;
    }
}
