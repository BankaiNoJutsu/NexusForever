using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Combat;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Combat;

public partial class ThreatManagerTests
{
    [Fact]
    public void OwnedRelations_ClearBothOriginalActorsAfterVisibilityIsGone()
    {
        var p = CreateOwnedThreatPair();
        p.OwnerThreat.UpdateThreat(p.Target, 10);
        p.OwnerProxy.SetMethodReturn(nameof(IUnitEntity.GetVisible), null);
        p.TargetProxy.SetMethodReturn(nameof(IUnitEntity.GetVisible), null);
        p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.GetVisible), _ => null);
        p.TargetProxy.SetMethodHandler(nameof(IUnitEntity.GetVisible), _ => null);
        p.OwnerThreat.RemoveHostile(p.Target.Guid);
        Assert.False(p.OwnerThreat.IsThreatened);
        Assert.False(p.TargetThreat.IsThreatened);
    }

    [Fact]
    public void OwnedRelations_GuidReuseCannotRedirectOriginalPeerCleanup()
    {
        var p = CreateOwnedThreatPair(); var replacement = CreateOwnedThreatPair();
        p.OwnerThreat.UpdateThreat(p.Target, 10);
        replacement.OwnerThreat.UpdateThreat(replacement.Target, 20);
        p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.GetVisible), _ => replacement.Target);
        p.OwnerThreat.RemoveHostile(p.Target.Guid);
        Assert.False(p.OwnerThreat.IsThreatened);
        Assert.False(p.TargetThreat.IsThreatened);
        Assert.Equal(20u, Assert.Single(replacement.OwnerThreat).Threat);
        Assert.Single(replacement.TargetThreat);
    }

    [Theory]
    [InlineData("packet")]
    [InlineData("callback")]
    public void OwnedRelations_ClearContinuesThroughRejectedPublicationAndScriptFailures(string failure)
    {
        var p = CreateOwnedThreatPair(); var other = NewOwnedThreatUnit(947);
        p.OwnerThreat.UpdateThreat(p.Target, 10);
        p.OwnerThreat.UpdateThreat(other.Unit, 20);
        p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.GetVisible), args =>
            (uint)args[0] == p.Target.Guid ? p.Target : other.Unit);
        if (failure == "packet")
            p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.EnqueueToVisible), _ => throw new IOException("Owned threat removal enqueue refusal."));
        else
            p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.OnThreatRemoveTarget), _ => throw new IOException("Owned threat removal script refusal."));
        Exception error = Record.Exception(p.OwnerThreat.ClearThreatList);
        Assert.Null(error);
        Assert.Empty(p.OwnerThreat);
        Assert.Empty(p.TargetThreat);
        Assert.Empty(other.Threat);
        Assert.Equal(2, p.OwnerProxy.GetInvocations(nameof(IUnitEntity.OnThreatRemoveTarget)).Count);
        var session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        p.OwnerThreat.SendThreatList(session);
        var packet = Assert.IsType<ServerEntityThreatListUpdate>(Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))).Arguments[0]);
        Assert.All(packet.ThreatUnitIds, id => Assert.Equal(0u, id));
        Assert.All(packet.ThreatLevels, value => Assert.Equal(0u, value));
    }

    [Fact]
    public void OwnedRelations_ReentrantRemovalCannotDeleteTheNewRelation()
    {
        var p = CreateOwnedThreatPair();
        p.OwnerThreat.UpdateThreat(p.Target, 10);
        var original = p.OwnerThreat.GetHostile(p.Target.Guid);
        int callbacks = 0;
        p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.OnThreatRemoveTarget), _ =>
        {
            if (++callbacks == 1) p.OwnerThreat.UpdateThreat(p.Target, 25);
            return null;
        });
        p.OwnerThreat.RemoveHostile(p.Target.Guid);
        var fresh = Assert.Single(p.OwnerThreat);
        Assert.NotSame(original, fresh);
        Assert.Equal(25u, fresh.Threat);
        Assert.Single(p.TargetThreat);
    }

    [Fact]
    public void OwnedRelations_FailedAddDoesNotRetainAnUnpairedCombatEntry()
    {
        var p = CreateOwnedThreatPair();
        p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.OnThreatAddTarget), _ =>
        {
            p.OwnerProxy.SetProperty(nameof(IUnitEntity.InCombat), true);
            throw new IOException("Owned threat admission script refusal.");
        });
        p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.OnThreatRemoveTarget), _ =>
        {
            p.OwnerProxy.SetProperty(nameof(IUnitEntity.InCombat), false);
            return null;
        });
        Exception error = Record.Exception(() => p.OwnerThreat.UpdateThreat(p.Target, 10));
        Assert.Null(error);
        Assert.Empty(p.OwnerThreat);
        Assert.Empty(p.TargetThreat);
        Assert.False(p.Owner.InCombat);
    }

    [Fact]
    public void OwnedRelations_TargetManagerReplacementRetiresOnlyTheOriginalManager()
    {
        var p = CreateOwnedThreatPair();
        p.OwnerThreat.UpdateThreat(p.Target, 10);
        var replacement = new ThreatManager(p.Target);
        p.TargetProxy.SetProperty(nameof(IUnitEntity.ThreatManager), replacement);
        p.OwnerThreat.RemoveHostile(p.Target.Guid);
        Assert.Empty(p.OwnerThreat);
        Assert.Empty(p.TargetThreat);
        Assert.Empty(replacement);
        Assert.Empty(p.TargetProxy.GetInvocations(nameof(IUnitEntity.OnThreatRemoveTarget)));
    }

    [Fact]
    public void OwnedRelations_ManagerChangedDuringAdmissionCannotReceiveAStaleReciprocalAdd()
    {
        var p = CreateOwnedThreatPair();
        var replacement = new ThreatManager(p.Target);
        p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.OnThreatAddTarget), _ =>
        {
            p.TargetProxy.SetProperty(nameof(IUnitEntity.ThreatManager), replacement);
            return null;
        });
        p.OwnerThreat.UpdateThreat(p.Target, 10);
        Assert.Empty(p.OwnerThreat);
        Assert.Empty(p.TargetThreat);
        Assert.Empty(replacement);
    }

    [Fact]
    public void OwnedRelations_VisibleOriginalPairStillCleansUp()
    {
        var p = CreateOwnedThreatPair();
        p.OwnerThreat.UpdateThreat(p.Target, 10);
        p.OwnerThreat.RemoveHostile(p.Target.Guid);
        Assert.Empty(p.OwnerThreat);
        Assert.Empty(p.TargetThreat);
    }


    [Fact]
    public void OwnedRelations_ClearSnapshotPreservesAReplacementForAnotherCapturedTarget()
    {
        var p = CreateOwnedThreatPair(); var other = NewOwnedThreatUnit(947);
        p.OwnerThreat.UpdateThreat(p.Target, 10); p.OwnerThreat.UpdateThreat(other.Unit, 20);
        int replacements = 0;
        p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.OnThreatRemoveTarget), args =>
        {
            if (((IHostileEntity)args[0]).HatedUnitId == p.Target.Guid && replacements++ == 0)
            {
                p.OwnerThreat.RemoveHostile(other.Unit.Guid);
                p.OwnerThreat.UpdateThreat(other.Unit, 30);
            }
            return null;
        });
        p.OwnerThreat.ClearThreatList();
        Assert.Empty(p.TargetThreat);
        Assert.Equal(30u, Assert.Single(p.OwnerThreat).Threat);
        Assert.Single(other.Threat);
    }

    [Fact]
    public void OwnedRelations_AnExplicitNewTargetWithAReusedGuidDoesNotInheritOldThreat()
    {
        var p = CreateOwnedThreatPair(); var fresh = NewOwnedThreatUnit(p.Target.Guid);
        p.OwnerThreat.UpdateThreat(p.Target, 100);
        p.OwnerThreat.UpdateThreat(fresh.Unit, 6);
        Assert.Empty(p.TargetThreat);
        Assert.Equal(6u, Assert.Single(p.OwnerThreat).Threat);
        Assert.Single(fresh.Threat);
        p.OwnerThreat.ClearThreatList();
        Assert.Empty(fresh.Threat);
    }

    [Fact]
    public void OwnedRelations_OwnerGuidChangeStillCleansTheOriginalPeerWithoutNewActorFeedback()
    {
        var p = CreateOwnedThreatPair();
        p.OwnerThreat.UpdateThreat(p.Target, 10);
        p.OwnerProxy.SetProperty(nameof(IUnitEntity.Guid), 999u);
        p.OwnerThreat.ClearThreatList();
        Assert.Empty(p.OwnerThreat);
        Assert.Empty(p.TargetThreat);
        Assert.Empty(p.OwnerProxy.GetInvocations(nameof(IUnitEntity.EnqueueToVisible)));
        Assert.Empty(p.OwnerProxy.GetInvocations(nameof(IUnitEntity.OnThreatRemoveTarget)));
    }

    [Fact]
    public void OwnedRelations_ChangeCallbackFailureKeepsTheUpdatedPairCoherent()
    {
        var p = CreateOwnedThreatPair(); p.OwnerThreat.UpdateThreat(p.Target, 10);
        p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.OnThreatChange), _ => throw new IOException("Owned threat-change callback refusal."));
        Assert.Null(Record.Exception(() => p.OwnerThreat.UpdateThreat(p.Target, 2)));
        Assert.Equal(12u, Assert.Single(p.OwnerThreat).Threat);
        Assert.Single(p.TargetThreat);
        p.OwnerThreat.ClearThreatList();
        Assert.Empty(p.OwnerThreat); Assert.Empty(p.TargetThreat);
    }

    [Fact]
    public void OwnedRelations_CompatibleExternalManagerRetainsItsOriginalEntryIdentity()
    {
        var p = CreateOwnedThreatPair();
        var external = RecordingDispatchProxy<IThreatManager>.Create(out var externalProxy);
        IHostileEntity entry = null;
        externalProxy.SetMethodHandler(nameof(IThreatManager.GetHostile), _ => entry);
        externalProxy.SetMethodHandler(nameof(IThreatManager.UpdateThreat), _ =>
        {
            entry = new HostileEntity(p.Owner.Guid); entry.UpdateThreat(1); return null;
        });
        externalProxy.SetMethodHandler(nameof(IThreatManager.RemoveHostile), _ => { entry = null; return null; });
        p.TargetProxy.SetProperty(nameof(IUnitEntity.ThreatManager), external);
        p.OwnerThreat.UpdateThreat(p.Target, 10);
        Assert.NotNull(entry);
        p.OwnerThreat.ClearThreatList();
        Assert.Empty(p.OwnerThreat); Assert.Null(entry);
        Assert.Single(externalProxy.GetInvocations(nameof(IThreatManager.RemoveHostile)));
    }


    [Fact]
    public void OwnedRelations_ReciprocalAdmissionReplacementCannotBeReboundByTheOlderRequest()
    {
        var p = CreateOwnedThreatPair(); int callbacks = 0;
        p.TargetProxy.SetMethodHandler(nameof(IUnitEntity.OnThreatAddTarget), _ =>
        {
            if (++callbacks == 1)
            {
                p.TargetThreat.RemoveHostile(p.Owner.Guid);
                p.OwnerThreat.UpdateThreat(p.Target, 7);
            }
            return null;
        });
        p.OwnerThreat.UpdateThreat(p.Target, 10);
        Assert.Equal(7u, Assert.Single(p.OwnerThreat).Threat);
        Assert.Single(p.TargetThreat);
        p.TargetThreat.ClearThreatList();
        Assert.Empty(p.TargetThreat);
        Assert.Empty(p.OwnerThreat);
    }

    [Fact]
    public void OwnedRelations_FailedExternalPeerLookupDoesNotRetainLocalAdmission()
    {
        var p = CreateOwnedThreatPair();
        var external = RecordingDispatchProxy<IThreatManager>.Create(out var externalProxy);
        externalProxy.SetMethodHandler(nameof(IThreatManager.GetHostile), _ => throw new IOException("Owned peer lookup refusal."));
        p.TargetProxy.SetProperty(nameof(IUnitEntity.ThreatManager), external);
        Assert.Null(Record.Exception(() => p.OwnerThreat.UpdateThreat(p.Target, 10)));
        Assert.Empty(p.OwnerThreat);
        Assert.Empty(p.TargetThreat);
        Assert.Empty(externalProxy.GetInvocations(nameof(IThreatManager.UpdateThreat)));
    }


    [Fact]
    public void OwnedRelations_PublicationReplacementCannotReceiveTheOlderRemovalCallback()
    {
        var p = CreateOwnedThreatPair();
        p.OwnerThreat.UpdateThreat(p.Target, 10);
        var replacement = new ThreatManager(p.Owner);
        p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.EnqueueToVisible), _ =>
        {
            p.OwnerProxy.SetProperty(nameof(IUnitEntity.ThreatManager), replacement);
            return null;
        });
        p.OwnerThreat.RemoveHostile(p.Target.Guid);
        Assert.Empty(p.OwnerThreat);
        Assert.Empty(p.TargetThreat);
        Assert.Empty(replacement);
        Assert.Empty(p.OwnerProxy.GetInvocations(nameof(IUnitEntity.OnThreatRemoveTarget)));
    }

    [Fact]
    public void OwnedRelations_InitialPeerLookupReplacementCannotInvokeTheOlderAddCallback()
    {
        var p = CreateOwnedThreatPair();
        var replacement = new ThreatManager(p.Owner);
        var external = RecordingDispatchProxy<IThreatManager>.Create(out var externalProxy);
        externalProxy.SetMethodHandler(nameof(IThreatManager.GetHostile), _ =>
        {
            p.OwnerProxy.SetProperty(nameof(IUnitEntity.ThreatManager), replacement);
            return null;
        });
        p.TargetProxy.SetProperty(nameof(IUnitEntity.ThreatManager), external);
        p.OwnerProxy.SetMethodHandler(nameof(IUnitEntity.OnThreatAddTarget), _ =>
        {
            p.OwnerProxy.SetProperty(nameof(IUnitEntity.InCombat), true);
            return null;
        });
        p.OwnerThreat.UpdateThreat(p.Target, 10);
        Assert.Empty(p.OwnerThreat);
        Assert.Empty(replacement);
        Assert.Empty(p.OwnerProxy.GetInvocations(nameof(IUnitEntity.OnThreatAddTarget)));
        Assert.False(p.Owner.InCombat);
    }

    private static OwnedThreatPair CreateOwnedThreatPair()
    {
        var a = NewOwnedThreatUnit(905); var b = NewOwnedThreatUnit(946);
        a.Proxy.SetMethodHandler(nameof(IUnitEntity.GetVisible), args => (uint)args[0] == b.Unit.Guid ? b.Unit : null);
        b.Proxy.SetMethodHandler(nameof(IUnitEntity.GetVisible), args => (uint)args[0] == a.Unit.Guid ? a.Unit : null);
        return new(a.Unit, a.Threat, a.Proxy, b.Unit, b.Threat, b.Proxy);
    }

    private static (IUnitEntity Unit, ThreatManager Threat, RecordingDispatchProxy<IUnitEntity> Proxy) NewOwnedThreatUnit(uint id)
    {
        var unit = RecordingDispatchProxy<IUnitEntity>.Create(out var proxy);
        proxy.SetProperty(nameof(IUnitEntity.Guid), id);
        var threat = new ThreatManager(unit);
        proxy.SetProperty(nameof(IUnitEntity.ThreatManager), threat);
        return (unit, threat, proxy);
    }

    private sealed record OwnedThreatPair(IUnitEntity Owner, ThreatManager OwnerThreat, RecordingDispatchProxy<IUnitEntity> OwnerProxy,
        IUnitEntity Target, ThreatManager TargetThreat, RecordingDispatchProxy<IUnitEntity> TargetProxy);
}
