using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_TRUE_LEVEL_20261107.md): prerequisite type 177 (evaluator case 0xB1) is NOT a challenge-presence lookup. With a non-null unit it calls the integer helper
// FUN_1404a2090(comparison, *(int*)(unit+0x38), Value): the unit's base level compared with Value, ObjectId unused. Type 1 (Level) reads +0x3C (the scaled/effective level) first and falls back to +0x38; 177 never
// uses +0x3C. Retail: 220 slots, ObjectId 0 (or 6548), Value 1, 10, 40, 50. The server has no separate effective level, so both read IUnitEntity.Level.
// Status: Implemented (disassembly); supersedes the earlier challenge-presence proxy.
public class PrerequisiteCheckTrueLevelTests
{
    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 50, 50u, true)]
    [InlineData(PrerequisiteComparison.GreaterThanOrEqual, 49, 50u, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, 50, 50u, false)]
    [InlineData(PrerequisiteComparison.LessThan, 9, 10u, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, 10, 10u, true)]
    [InlineData(PrerequisiteComparison.Equal, 40, 40u, true)]
    [InlineData(PrerequisiteComparison.NotEqual, 40, 40u, false)]
    [InlineData((PrerequisiteComparison)0, 40, 40u, false)]
    public void ComparesTheUnitLevelWithValue(PrerequisiteComparison comparison, int level, uint value, bool expected)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Level), (uint)level);

        Assert.Equal(expected, new PrerequisiteCheckTrueLevel().MeetsUnit(unit, comparison, value, 6548u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ObjectIdIsIgnoredAndANullUnitFails()
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Level), 50u);
        var check = new PrerequisiteCheckTrueLevel();

        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.Equal, 50u, 0u, new PrerequisiteParameters()));
        Assert.True(check.MeetsUnit(unit, PrerequisiteComparison.Equal, 50u, 12345u, new PrerequisiteParameters()));
        Assert.False(check.MeetsUnit(null, PrerequisiteComparison.Equal, 50u, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ThePlayerOverloadEvaluatesThePlayerAndDoesNotNeedAnNpcTarget()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Level), 50u);

        Assert.True(new PrerequisiteCheckTrueLevel().Meets(player, PrerequisiteComparison.GreaterThanOrEqual, 50u, 0u, new PrerequisiteParameters()));
    }
}
