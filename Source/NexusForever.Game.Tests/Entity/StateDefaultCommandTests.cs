using System.Numerics;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity.Movement.Command.State;
using NexusForever.Game.Static.Entity.Movement.Command.Mode;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Entity;

public class StateDefaultCommandTests
{
    [Theory]
    [InlineData(ModeType.Walk, StateFlags.ModeWalk)]
    [InlineData(ModeType.Swim, StateFlags.ModeNonWalk | StateFlags.ModeSwim)]
    [InlineData(ModeType.Slide, StateFlags.ModeNonWalk | StateFlags.ModeSlide)]
    [InlineData(ModeType.Free, StateFlags.ModeNonWalk)]
    public void GetState_EncodesMovementModeFlags(ModeType mode, StateFlags expectedFlags)
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetVelocity), Vector3.Zero);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetMove), Vector3.Zero);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetMode), mode);

        var command = new StateDefaultCommand();
        command.Initialise(movementManager);

        Assert.Equal(expectedFlags, command.GetState());
    }

    [Fact]
    public void GetState_IncludesVelocityAndMoveBitsWhenPresent()
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> movementProxy);
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetVelocity), new Vector3(1f, 0f, 0f));
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetMove), new Vector3(0f, 0f, 1f));
        movementProxy.SetMethodReturn(nameof(IMovementManager.GetMode), ModeType.Free);

        var command = new StateDefaultCommand();
        command.Initialise(movementManager);

        Assert.Equal(StateFlags.Velocity | StateFlags.Move | StateFlags.ModeNonWalk, command.GetState());
    }
}