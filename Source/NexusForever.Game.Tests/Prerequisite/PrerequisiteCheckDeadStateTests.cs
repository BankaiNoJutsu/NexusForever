using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_DEAD_STATE_BOOLEAN_20261107.md): prerequisite type 12 (FUN_14049c800) computes flag = (unit+0x250 != 0 || unit+0x254 != 0 || entityType(unit+0x80) == 0x17) and finishes
// with the boolean tail: Equal returns flag == Value, NotEqual returns flag != Value, any other comparison is false. 0x17 is EntityType.Ghost; the two fields are mapped to !IsAlive (inferred). Retail: 168 slots, comparisons
// Equal (159) and NotEqual (9), Value 0 or 1.
public class PrerequisiteCheckDeadStateTests
{
    [Theory]
    [InlineData(true, EntityType.Player, PrerequisiteComparison.Equal, 0u, true)]
    [InlineData(true, EntityType.Player, PrerequisiteComparison.Equal, 1u, false)]
    [InlineData(false, EntityType.Player, PrerequisiteComparison.Equal, 1u, true)]
    [InlineData(false, EntityType.Player, PrerequisiteComparison.NotEqual, 1u, false)]
    [InlineData(true, EntityType.Ghost, PrerequisiteComparison.Equal, 1u, true)]       // a ghost counts as dead even when the alive flag is set
    [InlineData(true, EntityType.Ghost, PrerequisiteComparison.NotEqual, 0u, true)]
    [InlineData(true, EntityType.Player, PrerequisiteComparison.GreaterThan, 0u, false)] // the boolean tail only knows Equal and NotEqual
    [InlineData(false, EntityType.Player, PrerequisiteComparison.GreaterThanOrEqual, 1u, false)]
    [InlineData(false, EntityType.Player, PrerequisiteComparison.LessThan, 5u, false)]
    public void ComparesTheDeadFlagWithValue(bool alive, EntityType type, PrerequisiteComparison comparison, uint value, bool expected)
    {
        IUnitEntity unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.IsAlive), alive);
        proxy.SetProperty(nameof(IUnitEntity.Type), type);

        Assert.Equal(expected, new PrerequisiteCheckDeadState().MeetsUnit(unit, comparison, value, 0u, new PrerequisiteParameters()));
    }

    [Fact]
    public void ANullUnitFailsAndThePlayerOverloadUsesTheTargetFirst()
    {
        var check = new PrerequisiteCheckDeadState();
        Assert.False(check.MeetsUnit(null, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
        playerProxy.SetProperty(nameof(IUnitEntity.Type), EntityType.Player);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out var targetProxy);
        targetProxy.SetProperty(nameof(IUnitEntity.IsAlive), false);
        targetProxy.SetProperty(nameof(IUnitEntity.Type), EntityType.NonPlayer);

        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 1u, 0u, new PrerequisiteParameters { Target = target }));
        Assert.True(check.Meets(player, PrerequisiteComparison.Equal, 0u, 0u, new PrerequisiteParameters()));
    }
}
