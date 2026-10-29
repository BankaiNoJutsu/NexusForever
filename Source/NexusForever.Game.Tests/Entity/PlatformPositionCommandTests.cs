using System.Numerics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Entity.Movement.Command.Position;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity.Movement.Command.Position;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.World.Entity.Command;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Entity;

public class PlatformPositionCommandTests
{
    [Theory]
    [InlineData(0f, 3f, 4f)]
    [InlineData(90f, 4f, -3f)]
    [InlineData(-90f, -4f, 3f)]
    [InlineData(180f, -3f, -4f)]
    public void PlatformYawTransformsWorldGridPositionWhileWireCoordinatesRemainRelative(float degrees, float x, float z)
    {
        var f = new Fixture();
        f.Platform.SetProperty(nameof(IWorldEntity.Rotation), new Vector3(degrees * MathF.PI / 180, 0, 0));
        Vector3 local = new(3, 2, 4);
        f.Group.SetPosition(local, false);

        f.Group.Update(1.1);

        var move = Assert.Single(f.Owner.GetInvocations(nameof(IWorldEntity.Relocate)));
        var world = Assert.IsType<Vector3>(move.Arguments[0]);
        Assert.Equal(100 + x, world.X, 3);
        Assert.Equal(12f, world.Y, 3);
        Assert.Equal(100 + z, world.Z, 3);
        Assert.True((bool)move.Arguments[1]);
        Assert.Equal(local, f.Group.GetPosition());
        Assert.Equal(local, Assert.IsType<SetPositionCommand>(f.Group.GetNetworkEntityCommand().Model).Position);
    }

    [Fact]
    public void MovingAndTurningPlatformRelocatesPassengerWithoutChangingItsLocalPosition()
    {
        var f = new Fixture();
        f.Group.SetPosition(new Vector3(3, 2, 4), false);
        f.Group.Update(1.1);
        f.Platform.SetProperty(nameof(IWorldEntity.Position), new Vector3(200, 20, 300));
        f.Platform.SetProperty(nameof(IWorldEntity.Rotation), new Vector3(MathF.PI / 2, 0, 0));
        f.Group.Update(1.1);
        var moves = f.Owner.GetInvocations(nameof(IWorldEntity.Relocate));
        Assert.Equal(2, moves.Count);
        var position = (Vector3)moves[1].Arguments[0];
        Assert.Equal(204f, position.X, 3);
        Assert.Equal(22f, position.Y, 3);
        Assert.Equal(297f, position.Z, 3);
        Assert.Equal(new Vector3(3, 2, 4), f.Group.GetPosition());
    }

    [Fact]
    public void ExistingOneSecondRelocationTimerIsPreserved()
    {
        var f = new Fixture();
        f.Group.SetPosition(Vector3.One, false);
        f.Group.Update(0.4);
        f.Group.Update(0.4);
        Assert.Empty(f.Owner.GetInvocations(nameof(IWorldEntity.Relocate)));
        f.Group.Update(0.3);
        Assert.Single(f.Owner.GetInvocations(nameof(IWorldEntity.Relocate)));
    }

    [Fact]
    public void MissingPlatformRetainsExistingPositionFallback()
    {
        var f = new Fixture();
        f.Map.SetMethodReturn(nameof(IBaseMap.GetEntity), null);
        f.Group.SetPosition(new Vector3(3, 2, 4), false);
        f.Group.Update(1.1);
        Assert.Equal(new Vector3(3, 2, 4), Assert.Single(f.Owner.GetInvocations(nameof(IWorldEntity.Relocate))).Arguments[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonFinitePlatformTransformNeverEnqueuesInvalidWorldPosition(bool invalidRotation)
    {
        var f = new Fixture();
        f.Platform.SetProperty(invalidRotation ? nameof(IWorldEntity.Rotation) : nameof(IWorldEntity.Position), new Vector3(float.NaN, 0, 0));
        f.Group.SetPosition(Vector3.One, false);
        f.Group.Update(1.1);
        Assert.Empty(f.Owner.GetInvocations(nameof(IWorldEntity.Relocate)));
        Assert.True(float.IsFinite(f.Group.GetPosition().X));
    }

    private sealed class Fixture
    {
        public PositionCommandGroup Group { get; }
        public RecordingDispatchProxy<IWorldEntity> Owner { get; }
        public RecordingDispatchProxy<IWorldEntity> Platform { get; }
        public RecordingDispatchProxy<IBaseMap> Map { get; }

        public Fixture()
        {
            var platform = RecordingDispatchProxy<IWorldEntity>.Create(out var pp);
            Platform = pp;
            pp.SetProperty(nameof(IWorldEntity.Position), new Vector3(100, 10, 100));
            var map = RecordingDispatchProxy<IBaseMap>.Create(out var mp);
            Map = mp;
            mp.SetMethodReturn(nameof(IBaseMap.GetEntity), platform);
            var owner = RecordingDispatchProxy<IWorldEntity>.Create(out var op);
            Owner = op;
            op.SetProperty(nameof(IWorldEntity.Guid), 1u);
            op.SetProperty(nameof(IWorldEntity.Map), map);
            var movement = RecordingDispatchProxy<IMovementManager>.Create(out var mm);
            mm.SetProperty(nameof(IMovementManager.Owner), owner);
            mm.SetProperty(nameof(IMovementManager.ServerControl), true);
            mm.SetMethodReturn(nameof(IMovementManager.GetPlatform), (uint?)2u);
            var factory = RecordingDispatchProxy<IFactoryInterface<IPositionCommand>>.Create(out var fp);
            fp.SetMethodReturnFactory("Resolve", () => new PositionCommand());
            Group = new PositionCommandGroup(factory, NullLogger<PositionCommandGroup>.Instance, null);
            Group.Initialise(movement);
        }
    }
}
