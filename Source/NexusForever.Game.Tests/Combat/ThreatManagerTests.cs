using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Combat;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Combat;

public class ThreatManagerTests
{
    [Fact]
    public void UpdateThreat_CreatesReciprocalHostile()
    {
        (IUnitEntity owner, IThreatManager ownerThreat, IUnitEntity target, IThreatManager targetThreat) = CreateThreatPair();

        ownerThreat.UpdateThreat(target, 1);

        Assert.NotNull(ownerThreat.GetHostile(target.Guid));
        Assert.NotNull(targetThreat.GetHostile(owner.Guid));
    }

    [Fact]
    public void UpdateThreat_WhenAddCallbackRemovesHostile_DoesNotCreateReciprocalHostile()
    {
        (IUnitEntity owner, IThreatManager ownerThreat, IUnitEntity target, IThreatManager targetThreat) = CreateThreatPair(
            onOwnerThreatAdd: (threatManager, targetId) => threatManager.RemoveHostile(targetId));

        ownerThreat.UpdateThreat(target, 1);

        Assert.Null(ownerThreat.GetHostile(target.Guid));
        Assert.Null(targetThreat.GetHostile(owner.Guid));
    }

    [Fact]
    public void UpdateThreat_WhenReciprocalAddCallbackRemovesHostile_RemovesOriginalHostile()
    {
        (IUnitEntity owner, IThreatManager ownerThreat, IUnitEntity target, IThreatManager targetThreat) = CreateThreatPair(
            onTargetThreatAdd: (threatManager, ownerId) => threatManager.RemoveHostile(ownerId));

        ownerThreat.UpdateThreat(target, 1);

        Assert.Null(ownerThreat.GetHostile(target.Guid));
        Assert.Null(targetThreat.GetHostile(owner.Guid));
    }

    [Fact]
    public void UpdateThreat_IgnoresOwnerAsTarget()
    {
        var threatPair = CreateThreatPair();
        IUnitEntity owner = threatPair.Owner;
        IThreatManager ownerThreat = threatPair.OwnerThreat;

        ownerThreat.UpdateThreat(owner, 1);
        ownerThreat.SetThreat(owner, 1);

        Assert.Null(ownerThreat.GetHostile(owner.Guid));
    }

    private static (IUnitEntity Owner, IThreatManager OwnerThreat, IUnitEntity Target, IThreatManager TargetThreat) CreateThreatPair(
        Action<IThreatManager, uint> onOwnerThreatAdd = null,
        Action<IThreatManager, uint> onTargetThreatAdd = null)
    {
        IUnitEntity owner = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> ownerProxy);
        IUnitEntity target = RecordingDispatchProxy<IUnitEntity>.Create(out RecordingDispatchProxy<IUnitEntity> targetProxy);

        IThreatManager ownerThreat = new ThreatManager(owner);
        IThreatManager targetThreat = new ThreatManager(target);

        ownerProxy.SetProperty(nameof(IUnitEntity.Guid), 905u);
        ownerProxy.SetProperty(nameof(IUnitEntity.ThreatManager), ownerThreat);
        targetProxy.SetProperty(nameof(IUnitEntity.Guid), 946u);
        targetProxy.SetProperty(nameof(IUnitEntity.ThreatManager), targetThreat);

        if (onOwnerThreatAdd != null)
        {
            ownerProxy.SetMethodHandler(nameof(IUnitEntity.OnThreatAddTarget), _ =>
            {
                onOwnerThreatAdd(ownerThreat, target.Guid);
                return null;
            });
        }

        if (onTargetThreatAdd != null)
        {
            targetProxy.SetMethodHandler(nameof(IUnitEntity.OnThreatAddTarget), _ =>
            {
                onTargetThreatAdd(targetThreat, owner.Guid);
                return null;
            });
        }

        return (owner, ownerThreat, target, targetThreat);
    }
}
