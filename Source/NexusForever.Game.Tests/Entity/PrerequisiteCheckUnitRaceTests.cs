using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

// Native evaluator (Decomp/Analysis/PREREQUISITE_TYPE_COVERAGE_20261107.md): type 196 (FUN_14049c520) reads the first int of the pointer at unit+0xd0 (the unit race id also read by TargetGroup
// types 12/13, 0 when the pointer is null) and compares it with Value: Equal returns equality, NotEqual inequality, others false; a missing unit is false. All 40 retail Values (33, 35, 61,
// 69 to 81, ...) are UnitRace ids, comparisons Equal 34 and NotEqual 6, ObjectId unused.
public class PrerequisiteCheckUnitRaceTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, 70u, true)]
    [InlineData(PrerequisiteComparison.Equal, 71u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 70u, false)]
    [InlineData(PrerequisiteComparison.NotEqual, 71u, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, 69u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 71u, false)]
    public void ComparesTheUnitsUnitRaceIdWithValue(PrerequisiteComparison comparison, uint value, bool expected)
    {
        IUnitEntity unit = Unit(new Creature2Entry { UnitRaceId = 70u });

        Assert.Equal(expected, new PrerequisiteCheckUnitRace().MeetsUnit(unit, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void AUnitWithoutACreatureRecordHasRaceZero()
    {
        IUnitEntity unit = Unit(null);

        Assert.True(new PrerequisiteCheckUnitRace().MeetsUnit(unit, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));
        Assert.False(new PrerequisiteCheckUnitRace().MeetsUnit(unit, PrerequisiteComparison.Equal, 70u, 0u, new PrerequisiteParameters()));
        Assert.True(new PrerequisiteCheckUnitRace().MeetsUnit(unit, PrerequisiteComparison.NotEqual, 70u, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ThePlayerEntryPointEvaluatesTheTargetWhenOneIsSupplied()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var parameters = new PrerequisiteParameters { Target = Unit(new Creature2Entry { UnitRaceId = 70u }) };

        Assert.True(new PrerequisiteCheckUnitRace().Meets(player, PrerequisiteComparison.Equal, 70u, 0u, parameters));
        Assert.False(new PrerequisiteCheckUnitRace().Meets(player, PrerequisiteComparison.Equal, 70u, 0u, new PrerequisiteParameters()));
    }

    private static IUnitEntity Unit(Creature2Entry entry)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.CreatureEntry), entry);
        return unit;
    }
}
