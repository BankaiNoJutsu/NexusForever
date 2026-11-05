using System.Data;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Internal;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model.Guild;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;

namespace NexusForever.Game.Tests.Guild;

public sealed partial class GuildAffiliationPublicationTests
{
    [Theory]
    [InlineData("failed-flush")]
    [InlineData("rollback")]
    [InlineData("lost-ack")]
    public async Task FailedOrUncertainSavesCannotPublishUntilTheOriginalAffiliationIsConfirmed(string fault)
    {
        using var f = await Create(); f.Manager.UpdateGuildAffiliation(8);
        if (fault == "rollback")
        {
            await using var context = f.Context(); await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            f.Actor.Player.Save(context); await context.SaveChangesAsync(); await transaction.RollbackAsync();
        }
        else await Assert.ThrowsAsync<IOException>(() => f.Save(fault == "lost-ack" ? new LostAcknowledgement() : new RejectFlush()));
        f.Pump(); Assert.Empty(f.Packets<ServerUnitGuildNameplateChanged>()); Assert.Empty(f.BrokerMessages());
        Assert.Equal("Circle7", f.Manager.PublishedAffiliation.Name);
        await f.Save(); f.Pump();
        Assert.Equal("Circle8", Assert.Single(f.Packets<ServerUnitGuildNameplateChanged>()).GuildName);
        Assert.Single(f.BrokerMessages());
    }

    [Fact]
    public async Task NormalSavePublishesItsFrozenSelectionAndRetainsALaterPublicRequest()
    {
        using var f = await Create(); f.Manager.UpdateGuildAffiliation(8);
        f.Actor.OnAuth = () => f.Manager.UpdateGuildAffiliation(7);
        await f.Actor.Player.SaveDirect(); f.Pump();
        Assert.Equal("Circle8", Assert.Single(f.Packets<ServerUnitGuildNameplateChanged>()).GuildName);
        Assert.Equal("Circle8", f.Manager.PublishedAffiliation.Name); Assert.Equal(7ul, f.Manager.GuildAffiliation.Id);
        f.Actor.OnAuth = null; await f.Actor.Player.SaveDirect(); f.Pump();
        Assert.Equal(new[] { "Circle8", "Circle7" }, f.Packets<ServerUnitGuildNameplateChanged>().Select(packet => packet.GuildName));
        Assert.Equal(new[] { "Circle8", "Circle7" }, f.BrokerMessages().Select(packet => packet.GuildName));
    }

    [Fact]
    public async Task EntityAndLoginModelsUsePublishedAffiliationWhileALaterSelectionWaits()
    {
        using var f = await Create(); f.Manager.UpdateGuildAffiliation(8);
        PlayerEntityModel Model() => (PlayerEntityModel)typeof(Player).GetMethod("BuildEntityModel", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(f.Actor.Player, null)!;
        Assert.Equal("Circle7", Model().GuildName);
        f.Manager.OnLogin(); Assert.Equal(0u, f.Packets<ServerGuildInit>().Last().ShowNameplateIndex);
        await f.Save(); Assert.Equal("Circle7", Model().GuildName);
        f.Pump(); Assert.Equal("Circle8", Model().GuildName);
        f.Manager.OnLogin(); var init = f.Packets<ServerGuildInit>().Last();
        Assert.Equal(1u, init.ShowNameplateIndex); Assert.Equal(0u, init.Self[0].RecruitmentAvailability); Assert.Equal(1u, init.Self[1].RecruitmentAvailability);
    }

    [Fact]
    public async Task CommittedNoticeKeepsItsAcceptedNameAndAnUnchangedIdCanStillPublishANewRequest()
    {
        using var f = await Create(); f.Manager.UpdateGuildAffiliation(8);
        ((RecordingDispatchProxy<IGuildBase>)(object)f.Manager.GuildAffiliation).SetProperty(nameof(IGuildBase.Name), "Later name");
        await f.Save(); f.Pump();
        Assert.Equal("Circle8", Assert.Single(f.Packets<ServerUnitGuildNameplateChanged>()).GuildName);
        f.Manager.UpdateGuildAffiliation(8); await f.Save(); f.Pump();
        Assert.Equal(new[] { "Circle8", "Later name" }, f.Packets<ServerUnitGuildNameplateChanged>().Select(packet => packet.GuildName));
    }

    [Theory]
    [InlineData("sync")]
    [InlineData("fault")]
    [InlineData("cancel")]
    public async Task BrokerFailuresRetryFreshMessagesWithoutReplayingAcceptedNameplates(string fault)
    {
        using var f = await Create(); bool reject = true; int accepted = 0;
        var attempted = new List<object>();
        f.Broker.SetMethodHandler(nameof(IInternalMessagePublisher.PublishAsync), args =>
        {
            attempted.Add(args[0]);
            if (!reject) { accepted++; return Task.CompletedTask; }
            if (fault == "sync") throw new IOException("Owned broker rejection.");
            return fault == "cancel" ? Task.FromCanceled(new CancellationToken(true)) : Task.FromException(new IOException("Owned broker task failure."));
        });
        f.Manager.UpdateGuildAffiliation(8); await f.Save(); f.Pump();
        Assert.Single(f.Packets<ServerUnitGuildNameplateChanged>()); Assert.Equal(0, accepted);
        reject = false; f.Pump();
        Assert.Equal(1, accepted); Assert.Single(f.Packets<ServerUnitGuildNameplateChanged>());
        Assert.Equal(attempted.Count, attempted.Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public async Task AnInFlightBrokerTaskRetainsFifoWithoutResendingOrAdvancingNewerPublication()
    {
        using var f = await Create(); var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int calls = 0;
        f.Broker.SetMethodHandler(nameof(IInternalMessagePublisher.PublishAsync), _ => ++calls == 1 ? pending.Task : Task.CompletedTask);
        f.Manager.UpdateGuildAffiliation(8); await f.Save(); f.Pump();
        f.Manager.UpdateGuildAffiliation(7); await f.Save(); f.Pump();
        Assert.Equal(1, calls); Assert.Single(f.Packets<ServerUnitGuildNameplateChanged>()); Assert.Equal("Circle8", f.Manager.PublishedAffiliation.Name);
        pending.SetResult(); f.Pump();
        Assert.Equal(2, calls);
        Assert.Equal(new[] { "Circle8", "Circle7" }, f.Packets<ServerUnitGuildNameplateChanged>().Select(packet => packet.GuildName));
        Assert.Equal("Circle7", f.Manager.PublishedAffiliation.Name);
    }

    [Theory]
    [InlineData("player")]
    [InlineData("queue")]
    [InlineData("epoch")]
    [InlineData("manager")]
    public async Task ReplacedPublicationOwnersCannotEmitTheCommittedOldActorsNotices(string replaced)
    {
        using var f = await Create(); f.Manager.UpdateGuildAffiliation(8); await f.Save();
        if (replaced == "player") f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        if (replaced == "queue") f.Messages.SetProperty(nameof(IWorldSession.Events), new EventQueue());
        if (replaced == "epoch") ((RecordingDispatchProxy<IAccount>)(object)f.Actor.Player.Account).SetProperty(nameof(IAccount.WorldSessionEpoch), f.Actor.Player.Account.WorldSessionEpoch + 1);
        if (replaced == "manager") f.Actor.Set(nameof(Player.GuildManager), RecordingDispatchProxy<IGuildManager>.Create(out _));
        f.Pump(); Assert.Empty(f.Packets<ServerUnitGuildNameplateChanged>()); Assert.Empty(f.BrokerMessages());
        using var stored = f.Context(); Assert.Equal(8ul, stored.Character.Single().GuildAffiliation);
    }

    [Fact]
    public async Task RebindingDuringAnAcceptedNameplateStopsRemainingBrokerAndRecipientSteps()
    {
        using var f = await Create();
        f.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerUnitGuildNameplateChanged) f.Messages.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
            return null;
        });
        f.Manager.UpdateGuildAffiliation(8); await f.Save(); f.Pump();
        Assert.Single(f.Packets<ServerUnitGuildNameplateChanged>()); Assert.Empty(f.BrokerMessages());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecipientRetryUsesFreshPacketsOrSkipsTheReplacedRecipient(bool replace)
    {
        using var f = await Create();
        var peer = RecordingDispatchProxy<IPlayer>.Create(out var player);
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var network);
        player.SetProperty(nameof(IPlayer.Identity), new Identity { Id = 90, RealmId = 1 }); player.SetProperty(nameof(IPlayer.Guid), 9000u);
        player.SetProperty(nameof(IPlayer.Session), session); network.SetProperty(nameof(IWorldSession.Player), peer); network.SetProperty(nameof(IWorldSession.Events), new EventQueue());
        var visible = (Dictionary<uint, IGridEntity>)typeof(GridEntity).GetField("visibleEntities", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(f.Actor.Player)!;
        visible.Add(9000, peer); var packets = new List<ServerUnitGuildNameplateChanged>(); bool reject = true;
        network.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerUnitGuildNameplateChanged packet)
            {
                packets.Add(packet); Assert.Equal("Circle8", packet.GuildName);
                if (reject) { packet.GuildName = "Rejected mutated packet"; throw new IOException("Owned peer rejection."); }
            }
            return null;
        });
        f.Manager.UpdateGuildAffiliation(8); await f.Save(); f.Pump(); int rejected = packets.Count;
        Assert.True(rejected > 0); Assert.Single(f.Packets<ServerUnitGuildNameplateChanged>());
        reject = false;
        if (replace) network.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        f.Pump(); Assert.Equal(rejected + (replace ? 0 : 1), packets.Count);
        Assert.Equal(packets.Count, packets.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Single(f.Packets<ServerUnitGuildNameplateChanged>()); Assert.Single(f.BrokerMessages());
    }

    [Fact]
    public async Task HolomarkChangesWaitForCommitAndKeepTheAcceptedStandardAndCurrentPlacementFlags()
    {
        using var f = await Create(guildTarget: true); f.Manager.UpdateGuildAffiliation(8);
        f.Manager.UpdateHolomark(false, false, false, true);
        Assert.Empty(f.Actor.Player.GetVisuals());
        f.Manager.Guild.Standard.ScanLines.GuildStandardPartEntry.ItemDisplayIdStandard = 999;
        await f.Save(); Assert.Empty(f.Actor.Player.GetVisuals()); f.Pump();
        var visuals = f.Actor.Player.GetVisuals().ToDictionary(row => row.Slot, row => row.DisplayId);
        Assert.Equal((ushort)101, visuals[ItemSlot.GuildStandardScanLines]); Assert.Equal((ushort)7163, visuals[ItemSlot.GuildStandardBack]);
        f.Manager.UpdateGuildAffiliation(7); Assert.NotEmpty(f.Actor.Player.GetVisuals());
        await f.Save(); f.Pump(); Assert.Empty(f.Actor.Player.GetVisuals());
    }

    [Fact]
    public async Task ReinitialisingDuringAnAcceptedNoticeCannotClearTheNewGenerationsCommittedPublication()
    {
        using var f = await Create(); bool reinitialise = true;
        f.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerUnitGuildNameplateChanged && reinitialise)
            {
                reinitialise = false;
                f.Manager.Initialise(f.Actor.Player, new NexusForever.Database.Character.Model.CharacterModel { Id = 42, GuildAffiliation = 8 });
                f.Manager.UpdateGuildAffiliation(7);
                // The callback deliberately re-enters and commits a new manager
                // generation before the old publication resumes its cleanup.
                f.Save().GetAwaiter().GetResult();
            }
            return null;
        });
        f.Manager.UpdateGuildAffiliation(8); await f.Save(); f.Pump();
        Assert.Equal(new[] { "Circle8", "Circle7" }, f.Packets<ServerUnitGuildNameplateChanged>().Select(packet => packet.GuildName));
        Assert.Equal("Circle7", Assert.Single(f.BrokerMessages()).GuildName);
        using var stored = f.Context(); Assert.Equal(7ul, stored.Character.Single().GuildAffiliation);
    }

    [Fact]
    public async Task NameplateRetryDoesNotAddressAUnitGuidThatWasReplacedAfterTheRejectedEnqueue()
    {
        using var f = await Create(); bool reject = true; int accepted = 0;
        f.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerUnitGuildNameplateChanged)
            {
                if (reject) throw new IOException("Owned old-unit rejection.");
                accepted++;
            }
            return null;
        });
        f.Manager.UpdateGuildAffiliation(8); await f.Save(); f.Pump();
        f.Actor.Set(nameof(Player.Guid), 4300u); reject = false; f.Pump();
        Assert.Equal(0, accepted); Assert.Single(f.BrokerMessages());
        Assert.Equal("Circle8", f.Manager.PublishedAffiliation.Name);
    }

    [Fact]
    public void LegacyAdapterKeepsItsImmediateLiveAffiliationPresentation()
    {
        var player = RecordingDispatchProxy<IPlayer>.Create(out var owner);
        owner.SetProperty(nameof(IPlayer.Identity), new Identity { Id = 42, RealmId = 1 }); owner.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var broker);
        broker.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        var global = RecordingDispatchProxy<IGlobalGuildManager>.Create(out var cache);
        var memberships = new[] { Circle(7), Circle(8) };
        cache.SetMethodReturn(nameof(IGlobalGuildManager.GetCharacterGuilds), memberships);
        var manager = new NexusForever.Game.Guild.GuildManager(publisher, globalGuildManager: global);
        owner.SetProperty(nameof(IPlayer.GuildManager), manager);
        manager.Initialise(player, new NexusForever.Database.Character.Model.CharacterModel { Id = 42, GuildAffiliation = 7 });
        manager.UpdateGuildAffiliation(8);
        Assert.Equal(8ul, manager.GuildAffiliation.Id); Assert.Equal(8ul, manager.PublishedAffiliation.Id);
        Assert.Single(broker.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync)));
    }

    [Fact]
    public async Task QuietLoadedAffiliationRepairAppliesItsCommittedHolomarkWithoutSendingSelectionNotices()
    {
        using var f = await Create(single: true, guildTarget: true, missingLoaded: true);
        f.Manager.OnLogin(); Assert.Empty(f.Actor.Player.GetVisuals());
        await f.Save(); Assert.Empty(f.Actor.Player.GetVisuals()); f.Pump();
        Assert.Equal(7ul, f.Manager.PublishedAffiliation.Id);
        Assert.Equal(7, f.Actor.Player.GetVisuals().Count());
        Assert.Empty(f.Packets<ServerUnitGuildNameplateChanged>()); Assert.Empty(f.BrokerMessages());
    }

    private sealed class RejectFlush : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => throw new IOException("Owned affiliation publication flush rejection.");
    }
    private sealed class LostAcknowledgement : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData data, CancellationToken cancellationToken = default)
            => Task.FromException(new IOException("Owned affiliation publication acknowledgement loss."));
    }
}
