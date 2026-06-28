using System.Numerics;
using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Entity;

public class ScannerUnitEntityTests
{
    [Fact]
    public void Update_WithVisibleOwnerBeyondRepathDistance_FollowsOwner()
    {
        IMovementManager movementManager = RecordingDispatchProxy<IMovementManager>.Create(out RecordingDispatchProxy<IMovementManager> movementProxy);
        var scanbot = new ScannerUnitEntity(movementManager)
        {
            SummonerGuid = 77u
        };
        SetPosition(scanbot, Vector3.Zero);

        IPlayer owner = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> ownerProxy);
        ownerProxy.SetProperty(nameof(IPlayer.Guid), 77u);
        ownerProxy.SetProperty(nameof(IPlayer.Position), new Vector3(10f, 0f, 0f));
        scanbot.AddVisible(owner);

        scanbot.Update(1.1d);

        RecordingDispatchProxy<IMovementManager>.Invocation follow =
            Assert.Single(movementProxy.GetInvocations(nameof(IMovementManager.Follow)));
        Assert.Same(owner, follow.Arguments[0]);
        Assert.Equal(3f, follow.Arguments[1]);
    }

    private static void SetPosition(GridEntity entity, Vector3 position)
    {
        typeof(GridEntity)
            .GetProperty(nameof(GridEntity.Position), BindingFlags.Instance | BindingFlags.Public)
            ?.SetValue(entity, position);
    }
}
