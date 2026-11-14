using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Entity;

// Verified native type196 default predicate14049c520: primary UnitRace ID at *[unit+0xd0] or0; full Value Equal/NotEqual, null false, ObjectId/relationship unread.
// Independent native Lua GetUnitRaceId confirms attribute identity. Source CreatureEntry bridge is retained with writer/dynamic assignment limits; see PREREQUISITE_196_PRIMARY_SUBJECT_20261114.md.
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
    public void ThePlayerEntryPointRetainsPrimaryWhenARelationshipIsSupplied()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        var parameters = new PrerequisiteParameters { Target = Unit(new Creature2Entry { UnitRaceId = 70u }) };

        Assert.False(new PrerequisiteCheckUnitRace().Meets(player, PrerequisiteComparison.Equal, 70u, 0u, parameters));
        Assert.True(new PrerequisiteCheckUnitRace().Meets(player, PrerequisiteComparison.Equal, 0u, 0u, parameters));
        Assert.True(new PrerequisiteCheckUnitRace().MeetsUnit(parameters.Target, PrerequisiteComparison.Equal, 70u, 0u, new PrerequisiteParameters { Target = player }));
        Assert.False(new PrerequisiteCheckUnitRace().Meets(player, PrerequisiteComparison.Equal, 70u, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData((PrerequisiteComparison)0)]
    [InlineData(PrerequisiteComparison.GreaterThan)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual)]
    [InlineData(PrerequisiteComparison.LessThan)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual)]
    [InlineData((PrerequisiteComparison)7)]
    public void UnsupportedRawModesFail(PrerequisiteComparison comparison)
    {
        Assert.False(new PrerequisiteCheckUnitRace().MeetsUnit(Unit(new Creature2Entry { UnitRaceId = 70u }), comparison, 70u, uint.MaxValue, null));
    }

    [Theory]
    [InlineData(0u, 0u, true)]
    [InlineData(uint.MaxValue, uint.MaxValue, true)]
    [InlineData(0x10046u, 70u, false)]
    [InlineData(70u, 0x10046u, false)]
    public void TheFullUnitRaceAndValueAreComparedWithoutReadingObjectId(uint unitRace, uint value, bool equal)
    {
        IUnitEntity unit = Unit(new Creature2Entry { UnitRaceId = unitRace });
        var check = new PrerequisiteCheckUnitRace();

        Assert.Equal(equal, check.MeetsUnit(unit, PrerequisiteComparison.Equal, value, unitRace, null));
        Assert.Equal(!equal, check.MeetsUnit(unit, PrerequisiteComparison.NotEqual, value, unitRace, null));
    }

    private static IUnitEntity Unit(Creature2Entry entry)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IWorldEntity.CreatureEntry), entry);
        return unit;
    }
}
