using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity.Movement.Command.State;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evaluator case 0x57 tests only primary +0x1070 & 2.
public sealed class MovingPrerequisiteNativeTests
{
    [Theory]
    [InlineData(StateFlags.None, 0f, 0f, false)]
    [InlineData(StateFlags.Velocity, 0f, 0f, false)]
    [InlineData(StateFlags.Move, 0f, 0f, true)]
    [InlineData(StateFlags.Move | StateFlags.Velocity, 0f, 0f, true)]
    [InlineData(StateFlags.Jump | StateFlags.Fall | StateFlags.DoubleJump, 0f, 0f, false)]
    [InlineData(StateFlags.None, 1f, 0f, false)]
    [InlineData(StateFlags.None, 0f, 1f, false)]
    [InlineData(StateFlags.Velocity, 1f, 1f, false)]
    [InlineData(StateFlags.Move, 1f, 1f, true)]
    [InlineData(StateFlags.Unknown04, 1f, 1f, false)]
    [InlineData(StateFlags.None, 0.01f, 0.01f, false)]
    [InlineData(StateFlags.None, 0.2f, 0f, false)]
    public void OnlyBit2DeterminesThePredicateRegardlessOfVectorMagnitudes(StateFlags state, float velocity, float move, bool expected)
    {
        IPlayer player = Player(state, velocity, move, out _, out _);
        var check = new PrerequisiteCheckMoving();

        Assert.Equal(expected, check.MeetsUnit(player, PrerequisiteComparison.Equal, uint.MaxValue, uint.MaxValue, null));
        Assert.Equal(!expected, check.MeetsUnit(player, PrerequisiteComparison.NotEqual, uint.MaxValue, uint.MaxValue, null));
    }

    [Theory]
    [InlineData((PrerequisiteComparison)0)]
    [InlineData((PrerequisiteComparison)7)]
    public void UnsupportedRawComparisonModesFail(PrerequisiteComparison comparison)
    {
        IPlayer player = Player(StateFlags.Move, 1f, 1f, out _, out _);
        Assert.False(new PrerequisiteCheckMoving().MeetsUnit(player, comparison, 0u, 0u, null));
    }

    [Theory]
    [InlineData(1f, 0f, false)]
    [InlineData(0f, 0.01f, true)]
    [InlineData(1f, 0.01f, true)]
    public void DefaultStateProducerStillDeterminesTheMoveBit(float velocity, float move, bool expected)
    {
        IPlayer player = Player(StateFlags.None, velocity, move, out var movement, out var movementProxy);
        var command = new StateDefaultCommand();
        command.Initialise(movement);
        movementProxy.SetMethodHandler(nameof(IMovementManager.GetState), _ => command.GetState());

        Assert.Equal(expected, new PrerequisiteCheckMoving().MeetsUnit(player, PrerequisiteComparison.Equal, 0u, 0u, null));
    }

    private static IPlayer Player(StateFlags state, float velocity, float move, out IMovementManager movement, out RecordingDispatchProxy<IMovementManager> movementProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        movement = RecordingDispatchProxy<IMovementManager>.Create(out movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetState), state);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetVelocity), new Vector3(velocity, 0f, 0f));
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetMove), new Vector3(move, 0f, 0f));
        proxy.SetProperty(nameof(IWorldEntity.MovementManager), movement);
        return player;
    }
}
