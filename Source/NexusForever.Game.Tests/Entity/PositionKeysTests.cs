using System.Numerics;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity.Movement.Key;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Entity;

public class PositionKeysTests
{
    [Fact]
    public void GetRotation_WhenCurrentSegmentHasNoDirection_ReturnsZeroRotation()
    {
        PositionKeys keys = CreatePositionKeys(
            time: 500u,
            times: [0u, 1000u, 2000u],
            positions:
            [
                Vector3.Zero,
                Vector3.Zero,
                Vector3.UnitX
            ]);

        Vector3 rotation = keys.GetRotation();

        Assert.Equal(Vector3.Zero, rotation);
    }

    [Fact]
    public void GetRotation_WhenCurrentTimeIsAtFinalKey_UsesLastSegment()
    {
        PositionKeys keys = CreatePositionKeys(
            time: 2000u,
            times: [0u, 1000u, 2000u],
            positions:
            [
                Vector3.Zero,
                Vector3.UnitX,
                Vector3.UnitX * 2f
            ]);

        Vector3 rotation = keys.GetRotation();

        Assert.True(float.IsFinite(rotation.X));
        Assert.True(float.IsFinite(rotation.Y));
        Assert.True(float.IsFinite(rotation.Z));
        Assert.Equal(-MathF.PI / 2f, rotation.X, 5);
    }

    private static PositionKeys CreatePositionKeys(uint time, List<uint> times, List<Vector3> positions)
    {
        uint currentTime = 0u;
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> movementProxy);
        movementProxy.SetMethodReturnFactory(nameof(IMovementManager.GetTime), () => currentTime);

        var keys = new PositionKeys();
        keys.Initialise(movementManager, times, positions);
        currentTime = time;
        return keys;
    }
}
