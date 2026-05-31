using System.Numerics;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Entity.Movement;
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
    public void ServerScaleCommands_AreAppliedWhenClientControlled()
    {
        MovementManagerHarness harness = MovementManagerHarness.Create();
        harness.Manager.ServerControl = false;

        harness.Manager.SetScaleKeys([0u, 1000u], [1.3f, 1f]);

        RecordingDispatchProxy<IScaleCommandGroup>.Invocation scaleInvocation = Assert.Single(harness.ScaleProxy.GetInvocations(nameof(IScaleCommandGroup.SetScaleKeys)));
        Assert.Equal([0u, 1000u], Assert.IsType<List<uint>>(scaleInvocation.Arguments[0]));
        Assert.Equal([1.3f, 1f], Assert.IsType<List<float>>(scaleInvocation.Arguments[1]));
    }

    [Fact]
    public void BroadcastNetworkEntityCommands_WithClientControlledScaleCommand_IncludesSelf()
    {
        MovementManagerHarness harness = MovementManagerHarness.Create(ownerGuid: 101u);
        harness.Manager.ServerControl = false;

        harness.Manager.SetScale(1.3f);
        harness.ScaleProxy.SetProperty(nameof(IScaleCommandGroup.IsDirty), true);

        harness.Manager.Update(0.016d);

        RecordingDispatchProxy<IUnitEntity>.Invocation enqueue = Assert.Single(harness.OwnerProxy.GetInvocations(nameof(IWorldEntity.EnqueueToVisible)));
        Assert.True(Assert.IsType<bool>(enqueue.Arguments[1]));
    }

    [Fact]
    public void ServerPositionPathCommands_WithInvalidNode_AreStopped()
    {
        MovementManagerHarness harness = MovementManagerHarness.Create();

        harness.Manager.SetPositionPath(
        [
            Vector3.Zero,
            new Vector3(float.NaN, 0f, 0f)
        ], SplineType.Linear, SplineMode.OneShot, 4f);

        Assert.Empty(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPositionPath)));

        var positionInvocation = Assert.Single(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPosition)));
        Assert.Equal(Vector3.Zero, Assert.IsType<Vector3>(positionInvocation.Arguments[0]));
        Assert.True(Assert.IsType<bool>(positionInvocation.Arguments[1]));
    }

    [Fact]
    public void ServerPositionKeys_WithInvalidPosition_AreStoppedBeforeBroadcast()
    {
        MovementManagerHarness harness = MovementManagerHarness.Create();

        harness.Manager.SetPositionKeys(
            [0u, 100u, 200u],
            [
                Vector3.Zero,
                Vector3.One,
                new Vector3(float.NaN, 0f, 0f)
            ]);

        Assert.Empty(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPositionKeys)));

        var positionInvocation = Assert.Single(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPosition)));
        Assert.Equal(Vector3.One, Assert.IsType<Vector3>(positionInvocation.Arguments[0]));
        Assert.True(Assert.IsType<bool>(positionInvocation.Arguments[1]));
    }

    [Fact]
    public void ServerPositionKeys_WithDecreasingTimes_AreStoppedBeforeBroadcast()
    {
        MovementManagerHarness harness = MovementManagerHarness.Create();

        harness.Manager.SetPositionKeys(
            [100u, 50u, 200u],
            [
                new Vector3(2f, 0f, 0f),
                new Vector3(3f, 0f, 0f),
                new Vector3(4f, 0f, 0f)
            ]);

        Assert.Empty(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPositionKeys)));

        var positionInvocation = Assert.Single(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPosition)));
        Assert.Equal(new Vector3(2f, 0f, 0f), Assert.IsType<Vector3>(positionInvocation.Arguments[0]));
        Assert.True(Assert.IsType<bool>(positionInvocation.Arguments[1]));
    }

    [Fact]
    public void ServerPositionPathCommands_WithZeroLengthPath_AreStopped()
    {
        MovementManagerHarness harness = MovementManagerHarness.Create();

        harness.Manager.SetPositionPath(
        [
            Vector3.One,
            Vector3.One
        ], SplineType.Linear, SplineMode.OneShot, 4f);

        Assert.Empty(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPositionPath)));

        var positionInvocation = Assert.Single(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPosition)));
        Assert.Equal(Vector3.One, Assert.IsType<Vector3>(positionInvocation.Arguments[0]));
        Assert.True(Assert.IsType<bool>(positionInvocation.Arguments[1]));
    }

    [Fact]
    public void LaunchPath_UsesPathGeneratorFromCurrentPosition()
    {
        MovementManagerHarness harness = MovementManagerHarness.Create();
        harness.PositionProxy.SetMethodReturn(nameof(IPositionCommandGroup.GetPosition), new Vector3(0f, 1f, 0f));

        harness.Manager.LaunchPath(new Vector3(5f, 5f, 0f), 4f);

        var pathInvocation = Assert.Single(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPositionPath)));
        var nodes = Assert.IsType<List<Vector3>>(pathInvocation.Arguments[0]);
        Assert.Equal(4, nodes.Count);
        AssertVector(nodes[0], 0f, 1f, 0f);
        AssertVector(nodes[1], 2f, 2.6f, 0f);
        AssertVector(nodes[2], 4f, 4.2f, 0f);
        AssertVector(nodes[3], 5f, 5f, 0f);
        Assert.Equal(SplineType.Linear, Assert.IsType<SplineType>(pathInvocation.Arguments[1]));
        Assert.Equal(SplineMode.OneShot, Assert.IsType<SplineMode>(pathInvocation.Arguments[2]));
        Assert.Equal(4f, Assert.IsType<float>(pathInvocation.Arguments[3]));
    }

    [Fact]
    public void Follow_WithInvalidTargetRotation_UsesFinitePathAndSpeed()
    {
        MovementManagerHarness harness = MovementManagerHarness.Create();
        harness.PositionProxy.SetMethodReturn(nameof(IPositionCommandGroup.GetPosition), Vector3.Zero);

        IWorldEntity target = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> targetProxy);
        IMovementManager targetMovement = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> targetMovementProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Guid), 2u);
        targetProxy.SetProperty(nameof(IWorldEntity.Position), new Vector3(10f, 0f, 0f));
        targetProxy.SetProperty(nameof(IWorldEntity.Rotation), new Vector3(float.NaN, 0f, 0f));
        targetProxy.SetProperty(nameof(IWorldEntity.MovementManager), targetMovement);
        targetMovementProxy.SetMethodReturn(nameof(IMovementManager.GetVelocity), new Vector3(float.NaN, 0f, 0f));

        harness.Manager.Follow(target, 2f);

        var pathInvocation = Assert.Single(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPositionPath)));
        var nodes = Assert.IsType<List<Vector3>>(pathInvocation.Arguments[0]);
        Assert.All(nodes, node =>
        {
            Assert.True(float.IsFinite(node.X));
            Assert.True(float.IsFinite(node.Y));
            Assert.True(float.IsFinite(node.Z));
        });
        Assert.Equal(SplineType.Linear, Assert.IsType<SplineType>(pathInvocation.Arguments[1]));
        Assert.Equal(SplineMode.OneShot, Assert.IsType<SplineMode>(pathInvocation.Arguments[2]));
        Assert.Equal(8f, Assert.IsType<float>(pathInvocation.Arguments[3]));
    }

    [Fact]
    public void Follow_WithDifferentFollowerGuids_SpreadsFinalPositionAroundTarget()
    {
        MovementManagerHarness first = MovementManagerHarness.Create(ownerGuid: 101u);
        first.PositionProxy.SetMethodReturn(nameof(IPositionCommandGroup.GetPosition), Vector3.Zero);
        MovementManagerHarness second = MovementManagerHarness.Create(ownerGuid: 103u);
        second.PositionProxy.SetMethodReturn(nameof(IPositionCommandGroup.GetPosition), Vector3.Zero);

        IWorldEntity target = CreateFollowTarget();

        first.Manager.Follow(target, 2f);
        second.Manager.Follow(target, 2f);

        Vector3 firstFinal = GetFinalPathNode(first);
        Vector3 secondFinal = GetFinalPathNode(second);
        Assert.NotEqual(firstFinal, secondFinal);
        Assert.Equal(2f, Vector2.Distance(new Vector2(target.Position.X, target.Position.Z), new Vector2(firstFinal.X, firstFinal.Z)), 4);
        Assert.Equal(2f, Vector2.Distance(new Vector2(target.Position.X, target.Position.Z), new Vector2(secondFinal.X, secondFinal.Z)), 4);
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

    private static IWorldEntity CreateFollowTarget()
    {
        IWorldEntity target = RecordingDispatchProxy<IWorldEntity>.Create(out RecordingDispatchProxy<IWorldEntity> targetProxy);
        IMovementManager targetMovement = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> targetMovementProxy);
        targetProxy.SetProperty(nameof(IWorldEntity.Guid), 2u);
        targetProxy.SetProperty(nameof(IWorldEntity.Position), new Vector3(10f, 0f, 0f));
        targetProxy.SetProperty(nameof(IWorldEntity.Rotation), Vector3.Zero);
        targetProxy.SetProperty(nameof(IWorldEntity.MovementManager), targetMovement);
        targetMovementProxy.SetMethodReturn(nameof(IMovementManager.GetVelocity), Vector3.Zero);
        return target;
    }

    private static Vector3 GetFinalPathNode(MovementManagerHarness harness)
    {
        var pathInvocation = Assert.Single(harness.PositionProxy.GetInvocations(nameof(IPositionCommandGroup.SetPositionPath)));
        var nodes = Assert.IsType<List<Vector3>>(pathInvocation.Arguments[0]);
        return nodes[^1];
    }

    private static void AssertVector(Vector3 actual, float expectedX, float expectedY, float expectedZ)
    {
        Assert.Equal(expectedX, actual.X, 5);
        Assert.Equal(expectedY, actual.Y, 5);
        Assert.Equal(expectedZ, actual.Z, 5);
    }

    private sealed class MovementManagerHarness
    {
        public MovementManager Manager { get; }
        public RecordingDispatchProxy<ITimeCommandGroup> TimeProxy { get; }
        public RecordingDispatchProxy<IPositionCommandGroup> PositionProxy { get; }
        public RecordingDispatchProxy<IVelocityCommandGroup> VelocityProxy { get; }
        public RecordingDispatchProxy<IMoveCommandGroup> MoveProxy { get; }
        public RecordingDispatchProxy<IScaleCommandGroup> ScaleProxy { get; }
        public RecordingDispatchProxy<IStateCommandGroup> StateProxy { get; }
        public RecordingDispatchProxy<IClientMovementCommandValidator> ValidatorProxy { get; }
        public RecordingDispatchProxy<IUnitEntity> OwnerProxy { get; }

        private MovementManagerHarness(
            MovementManager manager,
            RecordingDispatchProxy<ITimeCommandGroup> timeProxy,
            RecordingDispatchProxy<IPositionCommandGroup> positionProxy,
            RecordingDispatchProxy<IVelocityCommandGroup> velocityProxy,
            RecordingDispatchProxy<IMoveCommandGroup> moveProxy,
            RecordingDispatchProxy<IScaleCommandGroup> scaleProxy,
            RecordingDispatchProxy<IStateCommandGroup> stateProxy,
            RecordingDispatchProxy<IClientMovementCommandValidator> validatorProxy,
            RecordingDispatchProxy<IUnitEntity> ownerProxy)
        {
            Manager        = manager;
            TimeProxy      = timeProxy;
            PositionProxy  = positionProxy;
            VelocityProxy  = velocityProxy;
            MoveProxy      = moveProxy;
            ScaleProxy     = scaleProxy;
            StateProxy     = stateProxy;
            ValidatorProxy = validatorProxy;
            OwnerProxy     = ownerProxy;
        }

        public static MovementManagerHarness Create(uint activeCCStateMask = 0u, uint ownerGuid = 0u)
        {
            ITimeCommandGroup timeGroup = RecordingDispatchProxy<ITimeCommandGroup>.Create(out RecordingDispatchProxy<ITimeCommandGroup> timeProxy);
            IPlatformCommandGroup platformGroup = RecordingDispatchProxy<IPlatformCommandGroup>.Create(out _);
            IPositionCommandGroup positionGroup = RecordingDispatchProxy<IPositionCommandGroup>.Create(out RecordingDispatchProxy<IPositionCommandGroup> positionProxy);
            IVelocityCommandGroup velocityGroup = RecordingDispatchProxy<IVelocityCommandGroup>.Create(out RecordingDispatchProxy<IVelocityCommandGroup> velocityProxy);
            IMoveCommandGroup moveGroup = RecordingDispatchProxy<IMoveCommandGroup>.Create(out RecordingDispatchProxy<IMoveCommandGroup> moveProxy);
            IRotationCommandGroup rotationGroup = RecordingDispatchProxy<IRotationCommandGroup>.Create(out _);
            IScaleCommandGroup scaleGroup = RecordingDispatchProxy<IScaleCommandGroup>.Create(out RecordingDispatchProxy<IScaleCommandGroup> scaleProxy);
            IStateCommandGroup stateGroup = RecordingDispatchProxy<IStateCommandGroup>.Create(out RecordingDispatchProxy<IStateCommandGroup> stateProxy);
            IModeCommandGroup modeGroup = RecordingDispatchProxy<IModeCommandGroup>.Create(out _);
            IClientMovementCommandValidator validator = RecordingDispatchProxy<IClientMovementCommandValidator>.Create(out RecordingDispatchProxy<IClientMovementCommandValidator> validatorProxy);
            IUnitEntity owner = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> ownerProxy);
            IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);

            ownerProxy.SetProperty(nameof(IUnitEntity.Guid), ownerGuid);
            ownerProxy.SetProperty(nameof(IUnitEntity.Map), map);
            ownerProxy.SetProperty(nameof(IUnitEntity.ActiveCCStateMask), activeCCStateMask);
            ownerProxy.SetProperty(nameof(IUnitEntity.Position), Vector3.Zero);

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

            return new MovementManagerHarness(manager, timeProxy, positionProxy, velocityProxy, moveProxy, scaleProxy, stateProxy, validatorProxy, ownerProxy);
        }
    }
}
