using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement.AntiTamper;
using NexusForever.Game.Abstract.Entity.Movement.Command.Mode;
using NexusForever.Game.Abstract.Entity.Movement.Command.Move;
using NexusForever.Game.Abstract.Entity.Movement.Command.Platform;
using NexusForever.Game.Abstract.Entity.Movement.Command.Position;
using NexusForever.Game.Abstract.Entity.Movement.Command.Rotation;
using NexusForever.Game.Abstract.Entity.Movement.Command.Scale;
using NexusForever.Game.Abstract.Entity.Movement.Command.State;
using NexusForever.Game.Abstract.Entity.Movement.Command.Time;
using NexusForever.Game.Abstract.Entity.Movement.Command.Velocity;
using NexusForever.Game.Entity.Movement;
using NexusForever.Game.Static.Combat.CrowdControl;
using NexusForever.Game.Static.Entity.Movement.Command.State;
using NexusForever.Game.Static.Entity.Movement.Spline;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Entity;
using NexusForever.Network.World.Entity.Command;

namespace NexusForever.Game.Tests.Entity;

public class MovementManagerTests
{
    [Fact]
    public void HandleClientEntityCommands_MovementBlockSuppressesClientMotionAndFiltersState()
    {
        MovementManagerHarness harness = MovementManagerHarness.Create(1u << (int)CCState.Root);
        harness.Manager.ServerControl = false;

        StateFlags requestedState = StateFlags.Move | StateFlags.Jump | StateFlags.Fall;

        harness.Manager.HandleClientEntityCommands(
        [
            new NetworkEntityCommand
            {
                Model = new SetPositionCommand
                {
                    Position = new Vector3(4f, 5f, 6f),
                    Blend    = true
                }
            },
            new NetworkEntityCommand
            {
                Model = new SetVelocityCommand
                {
                    Velocity = new Vector3(7f, 8f, 9f),
                    Blend    = true
                }
            },
            new NetworkEntityCommand
            {
                Model = new SetMoveCommand
                {
                    Move  = new Vector3(0f, 0f, 1f),
                    Blend = false
                }
            },
            new NetworkEntityCommand
            {
                Model = new SetStateCommand
                {
                    State = requestedState
                }
            }
        ], 0u);

        Assert.Empty(harness.ValidatorProxy.GetInvocations(nameof(IClientMovementCommandValidator.ValidatePosition)));
        Assert.Empty(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPosition)));

        var velocityInvocation = Assert.Single(harness.VelocityProxy.GetInvocations(nameof(IVelocityCommandGroup.SetVelocity)));
        Assert.Equal(Vector3.Zero, Assert.IsType<Vector3>(velocityInvocation.Arguments[0]));
        Assert.True(Assert.IsType<bool>(velocityInvocation.Arguments[1]));

        var moveInvocation = Assert.Single(harness.MoveProxy.GetInvocations(nameof(IMoveCommandGroup.SetMove)));
        Assert.Equal(Vector3.Zero, Assert.IsType<Vector3>(moveInvocation.Arguments[0]));
        Assert.False(Assert.IsType<bool>(moveInvocation.Arguments[1]));

        var validateStateInvocation = Assert.Single(harness.ValidatorProxy.GetInvocations(nameof(IClientMovementCommandValidator.ValidateState)));
        Assert.Equal(requestedState, Assert.IsType<StateFlags>(validateStateInvocation.Arguments[0]));

        var stateInvocation = Assert.Single(harness.StateProxy.GetInvocations(nameof(IStateCommandGroup.SetState)));
        Assert.Equal(StateFlags.Fall, Assert.IsType<StateFlags>(stateInvocation.Arguments[0]));
    }

    [Fact]
    public void ServerPositionPathCommands_AreIgnoredWhenClientControlled()
    {
        MovementManagerHarness harness = MovementManagerHarness.Create();
        harness.Manager.ServerControl = false;

        harness.Manager.SetPositionPath(
        [
            Vector3.Zero,
            new Vector3(6f, 1f, 0f)
        ], SplineType.Linear, SplineMode.OneShot, 4f);
        harness.Manager.SetPositionMultiSpline([11, 12], SplineMode.Cyclic, 5f);
        harness.Manager.SetPositionProjectile(new Vector3(8f, 2f, 3f), Vector3.UnitY, TimeSpan.FromSeconds(1), 9.81f);

        Assert.Empty(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPositionPath)));
        Assert.Empty(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPositionMultiSpline)));
        Assert.Empty(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPositionProjectile)));
    }

    [Fact]
    public void HandleClientEntityCommands_ClientTimeSynchronisesMovementClock()
    {
        MovementManagerHarness harness = MovementManagerHarness.Create();
        harness.Manager.ServerControl = false;

        harness.Manager.HandleClientEntityCommands(
        [
            new NetworkEntityCommand
            {
                Model = new SetTimeCommand
                {
                    Time = 365339u
                }
            }
        ], 0u);

        var validateInvocation = Assert.Single(harness.ValidatorProxy.GetInvocations(nameof(IClientMovementCommandValidator.ValidateTime)));
        Assert.Equal(365339u, Assert.IsType<uint>(validateInvocation.Arguments[0]));

        var timeInvocation = Assert.Single(harness.TimeProxy.GetInvocations(nameof(ITimeCommandGroup.SetTime)));
        Assert.Equal(TimeSpan.FromMilliseconds(365339u), Assert.IsType<TimeSpan>(timeInvocation.Arguments[0]));
    }

    private sealed class MovementManagerHarness
    {
        public MovementManager Manager { get; }
        public RecordingDispatchProxy<ITimeCommandGroup> TimeProxy { get; }
        public RecordingDispatchProxy<IPositionCommandGroup> PositionProxy { get; }
        public RecordingDispatchProxy<IVelocityCommandGroup> VelocityProxy { get; }
        public RecordingDispatchProxy<IMoveCommandGroup> MoveProxy { get; }
        public RecordingDispatchProxy<IStateCommandGroup> StateProxy { get; }
        public RecordingDispatchProxy<IClientMovementCommandValidator> ValidatorProxy { get; }

        private MovementManagerHarness(
            MovementManager manager,
            RecordingDispatchProxy<ITimeCommandGroup> timeProxy,
            RecordingDispatchProxy<IPositionCommandGroup> positionProxy,
            RecordingDispatchProxy<IVelocityCommandGroup> velocityProxy,
            RecordingDispatchProxy<IMoveCommandGroup> moveProxy,
            RecordingDispatchProxy<IStateCommandGroup> stateProxy,
            RecordingDispatchProxy<IClientMovementCommandValidator> validatorProxy)
        {
            Manager        = manager;
            TimeProxy      = timeProxy;
            PositionProxy  = positionProxy;
            VelocityProxy  = velocityProxy;
            MoveProxy      = moveProxy;
            StateProxy     = stateProxy;
            ValidatorProxy = validatorProxy;
        }

        public static MovementManagerHarness Create(uint activeCCStateMask = 0u)
        {
            ITimeCommandGroup timeGroup = RecordingDispatchProxy<ITimeCommandGroup>.Create(out RecordingDispatchProxy<ITimeCommandGroup> timeProxy);
            IPlatformCommandGroup platformGroup = RecordingDispatchProxy<IPlatformCommandGroup>.Create(out _);
            IPositionCommandGroup positionGroup = RecordingDispatchProxy<IPositionCommandGroup>.Create(out RecordingDispatchProxy<IPositionCommandGroup> positionProxy);
            IVelocityCommandGroup velocityGroup = RecordingDispatchProxy<IVelocityCommandGroup>.Create(out RecordingDispatchProxy<IVelocityCommandGroup> velocityProxy);
            IMoveCommandGroup moveGroup = RecordingDispatchProxy<IMoveCommandGroup>.Create(out RecordingDispatchProxy<IMoveCommandGroup> moveProxy);
            IRotationCommandGroup rotationGroup = RecordingDispatchProxy<IRotationCommandGroup>.Create(out _);
            IScaleCommandGroup scaleGroup = RecordingDispatchProxy<IScaleCommandGroup>.Create(out _);
            IStateCommandGroup stateGroup = RecordingDispatchProxy<IStateCommandGroup>.Create(out RecordingDispatchProxy<IStateCommandGroup> stateProxy);
            IModeCommandGroup modeGroup = RecordingDispatchProxy<IModeCommandGroup>.Create(out _);
            IClientMovementCommandValidator validator = RecordingDispatchProxy<IClientMovementCommandValidator>.Create(out RecordingDispatchProxy<IClientMovementCommandValidator> validatorProxy);
            IUnitEntity owner = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> ownerProxy);

            ownerProxy.SetProperty(nameof(IUnitEntity.ActiveCCStateMask), activeCCStateMask);

            var manager = new MovementManager(
                timeGroup,
                platformGroup,
                positionGroup,
                velocityGroup,
                moveGroup,
                rotationGroup,
                scaleGroup,
                stateGroup,
                modeGroup,
                validator);

            manager.Initialise(owner);

            return new MovementManagerHarness(manager, timeProxy, positionProxy, velocityProxy, moveProxy, stateProxy, validatorProxy);
        }
    }
}
