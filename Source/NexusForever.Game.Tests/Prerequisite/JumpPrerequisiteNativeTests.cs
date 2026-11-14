using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evaluator case 0x56 tests only primary +0x1070 & 0x40.
public sealed class JumpPrerequisiteNativeTests
{
    [Theory]
    [InlineData(StateFlags.None, false)]
    [InlineData(StateFlags.Move, false)]
    [InlineData(StateFlags.Fall, false)]
    [InlineData(StateFlags.DoubleJump, false)]
    [InlineData(StateFlags.Fall | StateFlags.DoubleJump, false)]
    [InlineData(StateFlags.Jump, true)]
    [InlineData(StateFlags.Jump | StateFlags.Move | StateFlags.Fall | StateFlags.DoubleJump, true)]
    [InlineData(StateFlags.Jump | StateFlags.Unknown04, true)]
    public void OnlyBit40DeterminesThePredicateAndOperandsAreIgnored(StateFlags state, bool expected)
    {
        IPlayer player = Player(state);
        var check = new PrerequisiteCheckJump();

        Assert.Equal(expected, check.MeetsUnit(player, PrerequisiteComparison.Equal, uint.MaxValue, uint.MaxValue, null));
        Assert.Equal(!expected, check.MeetsUnit(player, PrerequisiteComparison.NotEqual, uint.MaxValue, uint.MaxValue, null));
    }

    [Theory]
    [InlineData((PrerequisiteComparison)0)]
    [InlineData((PrerequisiteComparison)7)]
    public void UnsupportedRawComparisonModesFail(PrerequisiteComparison comparison)
    {
        Assert.False(new PrerequisiteCheckJump().MeetsUnit(Player(StateFlags.Jump), comparison, 0u, 0u, null));
    }

    private static IPlayer Player(StateFlags state)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        IMovementManager movement = RecordingDispatchProxy<IMovementManager>.Create(out var movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), state);
        proxy.SetProperty(nameof(IWorldEntity.MovementManager), movement);
        return player;
    }
}
