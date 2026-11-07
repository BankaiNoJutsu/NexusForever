using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_UNIT_ENTITY_TYPE_20261107.md): prerequisite type 82 (FUN_14049c5b0) is false for a null unit, otherwise flag = (*(int*)(unit+0x80) == Value) and the boolean tail:
// Equal returns the flag, NotEqual its negation, every other comparison is false. The client unit type id uses the same numbering as EntityType (20 = Player, 23 = Ghost, as in types 39 and 12).
public class PrerequisiteCheckUnitEntityTypeTests
{
    [Theory]
    [InlineData(EntityType.Player, PrerequisiteComparison.Equal, 20u, true)]
    [InlineData(EntityType.Player, PrerequisiteComparison.Equal, 10u, false)]
    [InlineData(EntityType.Player, PrerequisiteComparison.NotEqual, 10u, true)]
    [InlineData(EntityType.Player, PrerequisiteComparison.NotEqual, 20u, false)]
    [InlineData(EntityType.Player, PrerequisiteComparison.GreaterThan, 10u, false)]            // an ordering comparison is false in the native tail
    [InlineData(EntityType.Player, PrerequisiteComparison.GreaterThanOrEqual, 20u, false)]
    [InlineData(EntityType.Player, PrerequisiteComparison.LessThanOrEqual, 20u, false)]
    public void ComparesTheEntityTypeWithValueThroughTheBooleanTail(EntityType type, PrerequisiteComparison comparison, uint value, bool expected)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Type), type);

        Assert.Equal(expected, new PrerequisiteCheckUnitEntityType().MeetsUnit(unit, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ANullUnitFailsEvenForNotEqual()
        => Assert.False(new PrerequisiteCheckUnitEntityType().MeetsUnit(null, PrerequisiteComparison.NotEqual, 20u, 0u, new PrerequisiteParameters()));
}
