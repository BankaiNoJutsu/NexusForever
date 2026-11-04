using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Numerics;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Database.Configuration.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Challenges;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable.Model;
using NexusForever.GameTable;
using NexusForever.Script;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Collection;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.Shared.Game;
using NexusForever.Shared.Game.Events;
using NexusForever.Network.Internal;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Game.Static.Entity;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;
using static NexusForever.Game.Tests.Challenges.ChallengeRewardPersistenceOwnershipTests;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using GameQuest = NexusForever.Game.Quest.Quest;

namespace NexusForever.Game.Tests.Challenges;

public sealed class RewardNormalSaveSnapshotTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public RewardNormalSaveSnapshotTests() { }
    internal RewardNormalSaveSnapshotTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Theory]
    [InlineData("success")]
    [InlineData("rollback")]
    [InlineData("lost-ack")]
    public async Task ResourceConversionPublishesBalancesOnlyAfterItsCharacterSaveIsConfirmed(string outcome)
    {
        using var f = new Fixture(options);
        f.Seed();
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables;
        tables.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(
            new CurrencyTypeEntry { Id = 1 }, new CurrencyTypeEntry { Id = 2 }, new CurrencyTypeEntry { Id = 3 }));
        tables.SetProperty(nameof(IGameTableManager.ResourceConversion), PlayerBindPointTests.Table(new ResourceConversionEntry
        {
            Id = 10, ResourceConversionTypeEnum = 4, SourceId = 1, SourceCount = 25, SurchargeId = 3,
            SurchargeCount = 3, TargetId = 2, TargetCount = 5
        }));
        var model = new CharacterModel { Id = 42 };
        model.Currency.Add(new() { Id = 42, CurrencyId = 1, Amount = 40 });
        model.Currency.Add(new() { Id = 42, CurrencyId = 2, Amount = 20 });
        model.Currency.Add(new() { Id = 42, CurrencyId = 3, Amount = 10 });
        using (var seed = f.Context())
        {
            seed.CharacterCurrency.AddRange(model.Currency);
            seed.SaveChanges();
        }

        var database = new ConversionSaveDatabase(f, outcome);
        var events = new EventQueue();
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var network);
        network.SetProperty(nameof(IWorldSession.Events), events);
        using var actor = new Actor(f, characterDatabase: database, actorSession: session, actorEvents: events);
        var player = actor.Player;
        network.SetProperty(nameof(IWorldSession.Player), player);
        await actor.Load();
        var wallet = new CurrencyManager(f.Source.Tables);
        wallet.Initialise(player, model);
        actor.Set(nameof(Player.CurrencyManager), wallet);
        actor.Quest.Single().Progress = 2;

        var request = new ClientConvertResource();
        typeof(ClientConvertResource).GetProperty(nameof(ClientConvertResource.ConversionId))!.SetValue(request, 10u);
        var notices = new List<ulong>();
        network.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerCombatReward balance) notices.Add(balance.NewValue);
            return null;
        });
        new ClientConvertResourceHandler(NullLogger<ClientConvertResourceHandler>.Instance, f.Source.Tables)
            .HandleMessage(session, request);

        Assert.Equal(new ulong[] { 15, 25, 7 }, wallet.OrderBy(currency => currency.Id).Select(currency => currency.Amount));
        Assert.Empty(notices);
        using (var before = f.Context())
            Assert.Equal(new ulong[] { 40, 20, 10 }, before.CharacterCurrency.OrderBy(row => row.CurrencyId).Select(row => row.Amount));
        await actor.Drain();

        Assert.Equal(1, database.Attempts);
        using var after = f.Context();
        Assert.Equal(outcome == "rollback" ? new ulong[] { 40, 20, 10 } : [15, 25, 7],
            after.CharacterCurrency.OrderBy(row => row.CurrencyId).Select(row => row.Amount));
        Assert.Equal(outcome == "rollback" ? 1u : 2u, after.CharacterQuestObjective.Single().Progress);
        Assert.Equal(outcome == "rollback" ? [] : new ulong[] { 7, 15, 25 }, notices.Order());
        Assert.Equal(outcome == "rollback" ? 1 : 0, network.GetInvocations(nameof(IWorldSession.ForceDisconnect)).Count);
        if (outcome == "rollback")
            Assert.Empty(after.BankReconciliationReceipt);
        else
        {
            var receipt = Assert.Single(after.BankReconciliationReceipt);
            using var payload = System.Text.Json.JsonDocument.Parse(receipt.Payload);
            Assert.Equal("normal-resource-conversion", payload.RootElement.GetProperty("Kind").GetString());
            Assert.Equal(42ul, payload.RootElement.GetProperty("Character").GetUInt64());
            Assert.Equal(3, payload.RootElement.GetProperty("Rows").EnumerateArray()
                .Count(row => row.GetProperty("Type").GetString()!.EndsWith(".CharacterCurrencyModel", StringComparison.Ordinal)));
        }
    }

    private sealed class ConversionSaveDatabase(Fixture fixture, string outcome) : CharacterDatabase
    {
        public int Attempts { get; private set; }

        public override Task SaveResourceConversionReconciliation(ulong characterId, Action<CharacterContext> action,
            ResourceConversionSaveReconciliation receipt)
        {
            Attempts++;
            return CharacterAtomicSave.ExecuteResourceConversionReconciliation(() => outcome == "success" ? fixture.Context()
                : fixture.Context(new CommitFault(outcome == "lost-ack")), characterId, action, receipt);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FrozenSaveBeforeTaxiFareCannotPersistThePaidDestinationEarly(bool replaced)
    {
        using var f = new Fixture(options);
        f.Seed();
        var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables;
        tables.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1, CapAmount = 1000 }));
        tables.SetProperty(nameof(IGameTableManager.TaxiNode), PlayerBindPointTests.Table(new TaxiNodeEntry { Id = 30 }));
        var walletRow = new CharacterCurrencyModel { Id = 42, CurrencyId = 1, Amount = 100 };
        using (var seed = f.Context())
        {
            seed.Character.Single().WorldId = 5;
            seed.CharacterCurrency.Add(walletRow);
            seed.SaveChanges();
        }
        var events = new EventQueue();
        var session = RecordingDispatchProxy<IWorldSession>.Create(out var sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Events), events);
        using var actor = new Actor(f, actorSession: session, actorEvents: events);
        var player = actor.Player;
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        await actor.Load();
        var model = new CharacterModel { Id = 42 };
        model.Currency.Add(walletRow);
        var wallet = new CurrencyManager(f.Source.Tables);
        wallet.Initialise(player, model);
        actor.Set(nameof(Player.CurrencyManager), wallet);
        typeof(Player).GetField("gameTableManager", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, f.Source.Tables);
        var destination = new NexusForever.Game.Map.MapPosition { Info = new NexusForever.Game.Map.MapInfo { Entry = new WorldEntry { Id = 7 } } };
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), destination.Info.Entry);
        typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(player, map);
        typeof(WorldEntity).GetProperty(nameof(WorldEntity.Zone))!.SetValue(player, new WorldZoneEntry { Id = 8 });
        typeof(Player).GetField("pendingTeleport", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player,
            new PendingTeleport { MapPosition = destination, TaxiFlight = new(player.Session, 30, 12, Vector3.Zero) });
        var admission = (IPlayerMapAdmission)player;
        ulong generation = admission.BeginMapAdmission();
        var state = (PlayerMapAdmissionState)typeof(Player).GetField("mapAdmissionState", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(player)!;
        Assert.Equal(generation, state.BeginBootstrap());

        actor.Quest.Single().Progress = 2;
        int completed = 0;
        player.Save(() => completed++);
        Assert.True(player.IsNormalSaveInProgress);
        int failedNotices = 0;
        var balances = new List<ulong>();
        var nodes = new List<uint>();
        sessionProxy.SetMethodHandler(nameof(IGameSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not (ServerCombatReward or ServerEntitySelectAuxUInt14)) return null;
            if (failedNotices < 2)
            {
                failedNotices++;
                throw new IOException("fixture fare publication failure");
            }
            if (args[0] is ServerCombatReward credit) balances.Add(credit.NewValue);
            if (args[0] is ServerEntitySelectAuxUInt14 unlock) nodes.Add(unlock.Value);
            return null;
        });
        Assert.True(player.TrySettleTaxiFare(destination, generation));
        admission.CompleteMapAdmission(generation, true);
        Assert.Equal(2, failedNotices);
        Assert.Equal(88ul, wallet.Cast<ICurrency>().Single().Amount);
        if (replaced)
            sessionProxy.SetProperty(nameof(IWorldSession.Player), RecordingDispatchProxy<IPlayer>.Create(out _));
        else
        {
            wallet.CurrencySubtractAmount(CurrencyType.Credits, 5);
            Assert.Equal(83ul, Assert.Single(balances));
            balances.Clear();
        }
        await actor.Drain();
        if (replaced)
        {
            Assert.Empty(balances);
            Assert.Empty(nodes);
        }
        else
        {
            Assert.Equal(83ul, Assert.Single(balances));
            Assert.Equal(30u, Assert.Single(nodes));
        }
        Assert.Equal(1, completed);
        using (var first = f.Context())
        {
            Assert.Equal((ushort)5, first.Character.Single().WorldId);
            Assert.Equal(100ul, first.CharacterCurrency.Single().Amount);
            Assert.Empty(first.CharacterTaxiNode);
            Assert.Equal(2u, first.CharacterQuestObjective.Single().Progress);
        }
        player.Save(() => completed++);
        await actor.Drain();
        Assert.Equal(2, completed);
        using var final = f.Context();
        Assert.Equal((ushort)7, final.Character.Single().WorldId);
        Assert.Equal(replaced ? 88ul : 83ul, final.CharacterCurrency.Single().Amount);
        Assert.Equal((ushort)30, final.CharacterTaxiNode.Single().TaxiNodeId);
    }

    [Theory]
    [InlineData("detached")]
    [InlineData("bootstrapping")]
    [InlineData("failed-bootstrap")]
    public async Task NormalSaveCommitsProgressWithoutReplacingLocationUntilDestinationBootstrapSucceeds(string phase)
    {
        using var f = new Fixture(options);
        f.Seed();
        using (var seed = f.Context())
        {
            var row = seed.Character.Single();
            row.WorldId = 5;
            row.WorldZoneId = 6;
            row.LocationX = 10;
            row.LocationY = 11;
            row.LocationZ = 12;
            row.RotationX = 0.5f;
            seed.SaveChanges();
        }

        using var actor = new Actor(f);
        await actor.Load();
        actor.Quest.Single().Progress = 2;
        var player = actor.Player;
        var admission = (IPlayerMapAdmission)player;
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 7 });
        var mapProperty = typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!;
        typeof(GridEntity).GetProperty(nameof(GridEntity.Position))!.SetValue(player, new Vector3(40, 50, 60));
        typeof(WorldEntity).GetProperty(nameof(WorldEntity.Zone))!.SetValue(player, new WorldZoneEntry { Id = 8 });
        ((RecordingDispatchProxy<IMovementManager>)(object)player.MovementManager)
            .SetMethodReturn(nameof(IMovementManager.GetRotation), new Vector3(1.5f, 2f, 3f));
        typeof(Player).GetField("saveMask", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(player, Player.PlayerSaveMask.Location);

        ulong generation = admission.BeginMapAdmission();
        var state = (PlayerMapAdmissionState)typeof(Player).GetField("mapAdmissionState", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(player)!;
        if (phase != "detached")
        {
            Assert.Equal(generation, state.BeginBootstrap());
            mapProperty.SetValue(player, map);
            if (phase == "failed-bootstrap") admission.CompleteMapAdmission(generation, false);
        }

        int completed = 0;
        player.Save(() => completed++);
        await actor.Drain();
        Assert.Equal(1, completed);
        using (var read = f.Context())
        {
            var row = read.Character.Single();
            Assert.Equal(((ushort)5, (ushort)6, 10f, 11f, 12f, 0.5f),
                (row.WorldId, row.WorldZoneId, row.LocationX, row.LocationY, row.LocationZ, row.RotationX));
            Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
        }

        if (phase != "bootstrapping")
        {
            if (phase == "failed-bootstrap") generation = admission.BeginMapAdmission();
            Assert.Equal(generation, state.BeginBootstrap());
            mapProperty.SetValue(player, map);
        }
        admission.CompleteMapAdmission(generation, true);
        player.Save(() => completed++);
        await actor.Drain();
        Assert.Equal(2, completed);
        using var final = f.Context();
        var arrived = final.Character.Single();
        Assert.Equal(((ushort)7, (ushort)8, 40f, 50f, 60f, 1.5f),
            (arrived.WorldId, arrived.WorldZoneId, arrived.LocationX, arrived.LocationY, arrived.LocationZ, arrived.RotationX));
    }

    [Theory]
    [InlineData(15u)] [InlineData(16u)] [InlineData(17u)] [InlineData(18u)]
    [InlineData(19u)] [InlineData(20u)] [InlineData(21u)] [InlineData(22u)]
    [InlineData(23u)] [InlineData(24u)] [InlineData(25u)] [InlineData(26u)]
    [InlineData(27u)] [InlineData(28u)] [InlineData(33u)] [InlineData(36u)]
    public async Task RealDirectSaveFreezesBeforeAuthAndTheCompletionQueueFollowsItsStoredBaseline(uint track)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f, track); using var actor = new Actor(f); await actor.Load();
        int calls = 0; actor.OnAuth = () =>
        {
            ++calls; Assert.True(actor.Player.IsNormalSaveInProgress); var capture = actor.Complete(); Assert.True(actor.QueueTerminal(capture, track));
        };
        await actor.Player.SaveDirect(); Assert.Equal(1, calls); Assert.False(actor.Player.IsNormalSaveInProgress);
        using (var read = f.Context()) { Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(0u, read.CharacterChallenge.Single().CompletionCount); }
        await actor.Drain();
        Assert.False(actor.Quests.RequiresAuthoritativeReload); Assert.False(actor.Challenges.RequiresAuthoritativeReload); Assert.False(actor.Quest.NeedsPersistenceSave);
        Assert.False(Runtime(actor.Challenges, f.Source.Challenge(track)).Activated);
        using var final = f.Context(); Assert.Equal(2u, final.CharacterQuestObjective.Single().Progress); Assert.Equal(1u, final.CharacterChallenge.Single().CompletionCount);
        Assert.Equal(3, final.CharacterChallengeMedalReceipt.Count()); Assert.Equal(300u, final.CharacterChallengeRewardTrack.Single().Points); Assert.Empty(final.Item);
    }

    [Fact]
    public async Task RealDeferredSaveKeepsItsFrozenTargetsAcrossTheAuthCharacterEventGap()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load(); int auth = 0;
        actor.OnAuth = () => { if (++auth == 1) Assert.True(actor.QueueTerminal(actor.Complete())); };
        actor.StartSave(); await actor.Drain();
        Assert.False(actor.Player.IsNormalSaveInProgress); Assert.False(actor.Challenges.RequiresAuthoritativeReload); Assert.False(actor.Quests.RequiresAuthoritativeReload);
        // The Quest event requested another save; its deferred dirty intent is
        // allowed to run once reward ownership has ended.
        actor.StartSave(); await actor.Drain(); Assert.Equal(2, auth);
        using var read = f.Context(); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(1u, read.CharacterChallenge.Single().CompletionCount);
    }

    [Fact]
    public async Task ScriptCreatedQuestIsExcludedFromTheOlderSaveAndCommittedOnlyWithItsCompletion()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); Actor actor = null; int calls = 0;
        var extra = ExtraInfo(9002, false);
        var script = RecordingDispatchProxy<IQuestScript>.Create(out var sp); sp.SetMethodHandler(nameof(IQuestScript.OnObjectiveUpdate), _ => { ++calls; actor.Quests.QuestMention(extra); return null; });
        var collection = RecordingDispatchProxy<IScriptCollection>.Create(out var cp); cp.SetMethodHandler(nameof(IScriptCollection.Invoke), (method, args) =>
        { if (method.IsGenericMethod && method.GetGenericArguments()[0] == typeof(IQuestScript)) ((Action<IQuestScript>)args[0])(script); return null; });
        ((RecordingDispatchProxy<IScriptManager>)(object)f.Scripts).SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), collection);
        using (actor = new Actor(f))
        {
            await actor.Load(); actor.OnAuth = () => Assert.True(actor.QueueTerminal(actor.Complete()));
            await actor.Player.SaveDirect(); using (var read = f.Context()) Assert.Single(read.CharacterQuest);
            await actor.Drain(); Assert.Equal(1, calls);
            using var final = f.Context(); Assert.Equal(2, final.CharacterQuest.Count()); Assert.Equal((byte)QuestState.Mentioned, final.CharacterQuest.Single(row => row.QuestId == 9002).State);
        }
    }

    [Fact]
    public async Task AQuestAcceptedAfterTheImmediateCompletionDoesNotReceiveTheOlderEvent()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load(); GameQuest later = null;
        actor.OnAuth = () =>
        {
            var capture = actor.Complete();
            later = new(actor.Player, ExtraInfo(9002, true), globalQuestManager: f.Global, scriptManager: f.Scripts, gameTableManager: f.Source.Tables);
            later.State = QuestState.Accepted; Active(actor.Quests).Add(9002, later); Assert.True(actor.QueueTerminal(capture));
        };
        await actor.Player.SaveDirect(); await actor.Drain(); Assert.Equal(0u, later.Single().Progress); Assert.True(later.NeedsPersistenceSave);
        using (var read = f.Context()) Assert.Single(read.CharacterQuest);
        actor.OnAuth = null; await actor.Player.SaveDirect(); using var final = f.Context(); Assert.Equal(0u, final.CharacterQuestObjective.Single(row => row.QuestId == 9002).Progress);
    }

    [Fact]
    public async Task SequentialObjectivesKeepTheirOriginalEventOrderAcrossTheAuthPhaseAndFifoCommits()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); await Ready(f, 17);
        ((RecordingDispatchProxy<IQuestInfo>)(object)f.Info).SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90010, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 2 }),
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90011, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 5,
                Flags = (uint)(QuestObjectiveFlags.RequiresPreviousObjectives | QuestObjectiveFlags.Sequential) })));
        using (var context = f.Context()) { context.CharacterQuestObjective.Add(new() { Id = 42, QuestId = Fixture.QuestId, Index = 1 }); context.SaveChanges(); }
        using var actor = new Actor(f); await actor.Load();
        actor.OnAuth = () =>
        {
            Assert.True(actor.QueueTerminal(actor.Complete())); Assert.Equal(new uint[] { 2, 0 }, actor.Quest.Select(row => row.Progress));
            Assert.True(actor.QueueTerminal(actor.Complete(), 17)); Assert.Equal(new uint[] { 2, 1 }, actor.Quest.Select(row => row.Progress));
        };
        await actor.Player.SaveDirect();
        using (var read = f.Context()) Assert.Equal(new uint[] { 1, 0 }, read.CharacterQuestObjective.OrderBy(row => row.Index).Select(row => row.Progress));
        await actor.Drain(); using var final = f.Context(); Assert.Equal(new uint[] { 2, 1 }, final.CharacterQuestObjective.OrderBy(row => row.Index).Select(row => row.Progress));
        Assert.Equal(2, final.CharacterChallenge.Count(row => row.CompletionCount == 1));
    }

    [Fact]
    public void LoadedObjectiveCollectionOrderCannotChangeSequentialProgression()
    {
        using var f = new Fixture(options);
        ((RecordingDispatchProxy<IQuestInfo>)(object)f.Info).SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90010, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 2 }),
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90011, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 5, Flags = (uint)QuestObjectiveFlags.Sequential })));
        var model = new CharacterQuestModel { Id = 42, QuestId = Fixture.QuestId, State = (byte)QuestState.Accepted,
            QuestObjective = new List<CharacterQuestObjectiveModel> { new() { Id = 42, QuestId = Fixture.QuestId, Index = 1, Progress = 0 }, new() { Id = 42, QuestId = Fixture.QuestId, Index = 0, Progress = 1 } } };
        var quest = new GameQuest(f.Source.Player, f.Info, model, globalQuestManager: f.Global, scriptManager: f.Scripts, gameTableManager: f.Source.Tables);
        Assert.Equal(new byte[] { 0, 1 }, quest.Select(row => row.Index));
        quest.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1); Assert.Equal(new uint[] { 2, 0 }, quest.Select(row => row.Progress));
        quest.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1); Assert.Equal(new uint[] { 2, 1 }, quest.Select(row => row.Progress));
    }

    [Fact]
    public async Task EventAfterCharacterStagingCannotChangeTheFrozenFlushOrAcknowledgeLaterIntent()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load();
        actor.Quest.Single().Progress = 2; actor.Challenges.Update(1);
        Assert.True(f.Source.Inventory.TryAcquireSaveLease(out var lease));
        var snapshot = actor.Player.CaptureNormalRewardSave(); Assert.NotNull(snapshot);
        await using (var context = f.Context())
        {
            snapshot.Quests.Save(context); snapshot.Challenges.Save(context);
            Assert.True(actor.QueueTerminal(actor.Complete())); actor.Quest.Single().Progress = 4;
            Assert.True(actor.Challenges.TryAdvanceProgress(f.Source.Challenge(16), 3));
            await context.SaveChangesAsync();
        }
        using (var read = f.Context()) { Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(0u, read.CharacterChallenge.Single().CurrentCount); Assert.Equal(299d, read.CharacterChallenge.Single().ActiveTimerSeconds); }
        Assert.True(snapshot.Finish(true)); lease.Dispose(); await actor.Drain();
        Assert.Equal(4u, actor.Quest.Single().Progress); Assert.True(actor.Quest.NeedsPersistenceSave);
        await actor.Player.SaveDirect();
        using var final = f.Context(); Assert.Equal(4u, final.CharacterQuestObjective.Single().Progress); Assert.Equal(3u, final.CharacterChallenge.Single().CurrentCount);
    }

    [Fact]
    public async Task AProbeCannotRunUntilTheFrozenNormalSaveReleasesItsOlderTargets()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load();
        var snapshot = actor.Player.CaptureNormalRewardSave(); using var capture = actor.Complete();
        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.PrepareAsync(() => f.Context()));
        await using (var context = f.Context()) { snapshot.Quests.Save(context); snapshot.Challenges.Save(context); await context.SaveChangesAsync(); }
        Assert.True(snapshot.Finish(true)); var prepared = await capture.PrepareAsync(() => f.Context());
        Assert.Equal(1u, prepared.Changes[0].Before.Objectives[0].Progress); Assert.Equal(2u, prepared.Changes[0].After.Objectives[0].Progress);
    }

    [Fact]
    public async Task SameValueTouchesAfterCaptureStayDirtyUntilANewerSaveAcknowledgesThem()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); actor.Quest.Single().Progress = 2;
        var snapshot = actor.Player.CaptureNormalRewardSave(); actor.Quest.Single().Progress = 2;
        await using (var context = f.Context()) { snapshot.Quests.Save(context); snapshot.Challenges.Save(context); await context.SaveChangesAsync(); }
        Assert.True(snapshot.Finish(true)); Assert.True(actor.Quest.NeedsPersistenceSave);
        await actor.Player.SaveDirect(); Assert.False(actor.Quest.NeedsPersistenceSave);
    }

    [Fact]
    public async Task AReplacementAfterCaptureIsNotWrittenOrAcknowledgedByTheOlderNormalSave()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load(); actor.Quest.Single().Progress = 2;
        var old = actor.Quest; var snapshot = actor.Player.CaptureNormalRewardSave();
        var replacement = f.New(); replacement.Single().Progress = 4;
        using var capture = actor.Complete(() => Active(actor.Quests)[Fixture.QuestId] = replacement);
        await using (var context = f.Context()) { snapshot.Quests.Save(context); snapshot.Challenges.Save(context); await context.SaveChangesAsync(); }
        Assert.True(snapshot.Finish(true)); using (var read = f.Context()) Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
        Assert.True(actor.QueueTerminal(capture)); await actor.Drain(); Assert.Same(replacement, actor.Quest); Assert.False(replacement.NeedsPersistenceSave);
        using var denied = f.Context(); Assert.Throws<InvalidOperationException>(() => old.Save(denied));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ADeletionSnapshotPreservesLaterRestorationAndRetiresOnlyTheCommittedDeletedOwner(bool restore)
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var quest = actor.Quest; quest.EnqueueDelete(true);
        var snapshot = actor.Player.CaptureNormalRewardSave(); if (restore) { quest.EnqueueDelete(false); quest.Single().Progress = 3; }
        await using (var context = f.Context()) { snapshot.Quests.Save(context); snapshot.Challenges.Save(context); await context.SaveChangesAsync(); }
        Assert.True(snapshot.Finish(true)); using (var read = f.Context()) { Assert.Empty(read.CharacterQuest); Assert.Empty(read.CharacterQuestObjective); }
        if (restore) { Assert.Same(quest, actor.Quest); await actor.Player.SaveDirect(); using var read = f.Context(); Assert.Equal(3u, read.CharacterQuestObjective.Single().Progress); }
        else Assert.Empty(actor.Quests.GetActiveQuests());
    }

    [Fact]
    public async Task ForeignManagersAndDirectLeafSavesCannotEnterTheAuthPhaseReservation()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var snapshot = actor.Player.CaptureNormalRewardSave();
        using var foreign = f.Manager(); var quest = Assert.IsType<GameQuest>(Assert.Single(foreign.GetActiveQuests())); quest.Single().Progress = 4;
        using (var context = f.Context()) { Assert.Throws<InvalidOperationException>(() => quest.Single().Save(context)); Assert.True(context.IsSaveRejected); }
        using (var identity = f.Context()) Assert.False(foreign.TryCaptureRewardCompletion(Game.Entity.Item.GetRewardDatabaseKey(identity), () => throw new Exception("must not run"), out _));
        Assert.Throws<InvalidOperationException>(() => foreign.CaptureNormalSave(actor.Identity.ExecutionDatabase));
        Assert.True(snapshot.Finish(true));
    }

    [Fact]
    public async Task SnapshotDatabaseAndContextBindingRejectsForeignOrRepeatedEnrollment()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); var snapshot = actor.Player.CaptureNormalRewardSave();
        using (var wrong = new CharacterContext(new DbContextOptionsBuilder<CharacterContext>().UseSqlite("Data Source=:memory:").Options))
        { Assert.Throws<InvalidOperationException>(() => snapshot.Quests.Save(wrong)); Assert.True(wrong.IsSaveRejected); }
        await using (var context = f.Context())
        {
            snapshot.Quests.Save(context); snapshot.Challenges.Save(context);
            using var other = f.Context(); Assert.Throws<InvalidOperationException>(() => snapshot.Challenges.Save(other)); Assert.True(other.IsSaveRejected);
            await context.SaveChangesAsync();
        }
        Assert.True(snapshot.Finish(true)); using var ended = f.Context(); Assert.Throws<InvalidOperationException>(() => snapshot.Quests.Save(ended)); Assert.True(ended.IsSaveRejected);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task FailedNormalCommitCannotLeakProvisionalCompletionThroughAnOrdinaryRetry(bool after)
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load(); actor.Quest.Single().Progress = 2;
        Assert.True(f.Source.Inventory.TryAcquireSaveLease(out var lease)); var snapshot = actor.Player.CaptureNormalRewardSave(); using var capture = actor.Complete(); Assert.True(actor.QueueTerminal(capture));
        await using (var context = f.Context(new CommitFault(after)))
        {
            await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            snapshot.Quests.Save(context); snapshot.Challenges.Save(context); await context.SaveChangesAsync(); await Assert.ThrowsAsync<IOException>(() => unit.CommitAsync());
        }
        Assert.False(snapshot.Finish(false)); lease.Dispose(); await actor.Drain();
        Assert.True(actor.Quests.RequiresAuthoritativeReload); Assert.True(actor.Challenges.RequiresAuthoritativeReload);
        Assert.False(actor.Quests.HasPendingRewardOperation); using var read = f.Context(); Assert.Equal(after ? 2u : 1u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count());
    }

    [Fact]
    public async Task AuthFailureAfterCaptureCancelsTheQueueBeforeReleasingTheRealDirectSave()
    {
        using var f = new Fixture(options); f.Seed(); await Ready(f); using var actor = new Actor(f); await actor.Load();
        actor.OnAuth = () => { Assert.True(actor.QueueTerminal(actor.Complete())); throw new IOException("Owned Auth failure."); };
        await Assert.ThrowsAsync<IOException>(() => actor.Player.SaveDirect()); await actor.Drain();
        Assert.False(actor.Player.IsNormalSaveInProgress); Assert.True(actor.Quests.RequiresAuthoritativeReload); Assert.True(actor.Challenges.RequiresAuthoritativeReload);
        using var read = f.Context(); Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress); Assert.Equal(2, read.CharacterChallengeMedalReceipt.Count());
    }

    [Fact]
    public async Task QuiescentRewardInvalidationStillAllowsAnOrdinaryLogoutSaveWithoutReopeningRewards()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); actor.Quest.Single().Progress = 2;
        f.Source.Inventory.InvalidateRewardDeliveries(); actor.Challenges.InvalidateRewardOperations(); actor.Quests.InvalidateRewardOperations();
        await actor.Player.SaveDirect(); Assert.False(actor.Player.IsNormalSaveInProgress);
        bool called = false; Assert.False(actor.Quests.TryCaptureRewardCompletion(actor.Identity.ExecutionDatabase, () => called = true, out _)); Assert.False(called);
        using var read = f.Context(); Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
    }

    [Theory]
    [InlineData("none")] [InlineData("auth")] [InlineData("snapshot")]
    public async Task RealCleanupFinalizesOnceAcrossNormalSaveFailureAndSynchronousSnapshotFailure(string fault)
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); actor.Quest.Single().Progress = 2; int auth = 0;
        actor.OnAuth = () => { ++auth; if (fault == "auth") throw new IOException("Owned cleanup Auth failure."); };
        if (fault == "snapshot") Assert.NotNull(actor.Player.CaptureNormalRewardSave());
        actor.Cleanup(); await actor.Drain(allowErrors: fault == "snapshot");
        Assert.Single(actor.CleanupProxy.GetInvocations(nameof(ICleanupManager.RemovePlayer)));
        Assert.Single(actor.PublisherProxy.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync)), call => call.Arguments[0] is PlayerLoggedOutMessage);
        Assert.Equal(LogoutState.Finished, actor.Player.LogoutManager.State); Assert.Equal(fault == "snapshot" ? 0 : 1, auth);
        if (fault == "snapshot") Assert.Single(actor.Errors); else Assert.Empty(actor.Errors);
        using var read = f.Context(); Assert.Equal(fault == "none" ? 2u : 1u, read.CharacterQuestObjective.Single().Progress);
    }

    [Fact]
    public async Task FinishingBeforeTheRealCharacterUnitEndsCannotReleaseItsPersistenceOwnership()
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load(); actor.Quest.Single().Progress = 2;
        var snapshot = actor.Player.CaptureNormalRewardSave();
        await using (var context = f.Context())
        {
            snapshot.Quests.Save(context); snapshot.Challenges.Save(context); Assert.False(snapshot.Finish(true));
            using var foreign = f.Manager(); Assert.False(foreign.TryCaptureRewardCompletion(actor.Identity.ExecutionDatabase, () => { }, out _));
        }
        Assert.True(actor.Quests.RequiresAuthoritativeReload); Assert.True(actor.Challenges.RequiresAuthoritativeReload);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ObjectiveOrdinalsAreExplicitEvenWhenZeroIsAddedAfterAnAlreadyTrackedOne(bool frozen)
    {
        using var f = new Fixture(options); f.Seed(); using var actor = new Actor(f); await actor.Load();
        var info = RecordingDispatchProxy<IQuestInfo>.Create(out var qp); qp.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = 9002 });
        qp.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90020, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 5 }),
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90021, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 5 })));
        var quest = new GameQuest(actor.Player, info, globalQuestManager: f.Global, scriptManager: f.Scripts, gameTableManager: f.Source.Tables); Active(actor.Quests).Add(9002, quest);
        if (frozen)
        {
            var snapshot = actor.Player.CaptureNormalRewardSave();
            await using var context = f.Context(); snapshot.Quests.Save(context); snapshot.Challenges.Save(context); await context.SaveChangesAsync(); Assert.True(snapshot.Finish(true));
        }
        else
        {
            await using var context = f.Context(); quest.ElementAt(1).Save(context); quest.Save(context);
            var zero = context.ChangeTracker.Entries<CharacterQuestObjectiveModel>().Single(row => row.Entity.QuestId == 9002 && row.Entity.Index == 0);
            Assert.False(zero.Property(row => row.Index).IsTemporary); await context.SaveChangesAsync();
        }
        using var read = f.Context(); var property = read.Model.FindEntityType(typeof(CharacterQuestObjectiveModel))!.FindProperty(nameof(CharacterQuestObjectiveModel.Index))!;
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never, property.ValueGenerated); Assert.Equal((byte)0, property.GetDefaultValue());
        Assert.Equal(new byte[] { 0, 1 }, read.CharacterQuestObjective.Where(row => row.QuestId == 9002).OrderBy(row => row.Index).Select(row => row.Index));
    }

    private static async Task Ready(Fixture f, uint track = 16) { await f.Source.Begin(track); await f.Source.Medal(track, 0, new(1, 1)); await f.Source.Medal(track, 1, new(1, 2)); }
    private static Dictionary<ushort, IQuest> Active(QuestManager manager) => (Dictionary<ushort, IQuest>)typeof(QuestManager).GetField("activeQuests", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(manager)!;
    private static IQuestInfo ExtraInfo(uint id, bool objective)
    {
        var info = RecordingDispatchProxy<IQuestInfo>.Create(out var proxy); proxy.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = id });
        proxy.SetProperty(nameof(IQuestInfo.Objectives), objective ? ImmutableList.Create<IQuestObjectiveInfo>(new QuestObjectiveInfo(new QuestObjectiveEntry { Id = id * 10, Type = (uint)QuestObjectiveType.CompleteChallenge, Count = 5 })) : ImmutableList<IQuestObjectiveInfo>.Empty); return info;
    }
    internal sealed class Actor : IDisposable
    {
        private readonly Fixture f;
        internal readonly Player Player;
        internal readonly QuestManager Quests;
        internal readonly ChallengeManager Challenges;
        internal readonly EventQueue Events;
        internal readonly List<Exception> Errors = [];
        internal readonly RecordingDispatchProxy<ICleanupManager> CleanupProxy;
        internal readonly RecordingDispatchProxy<IInternalMessagePublisher> PublisherProxy;
        internal readonly ChallengeManager.RewardPersistenceIdentity Identity;
        internal Action OnAuth;
        internal GameQuest Quest => Assert.IsType<GameQuest>(Assert.Single(Quests.GetActiveQuests()));
        internal Actor(Fixture fixture, IPrerequisiteManager prerequisites = null, Func<CharacterContext> rewardContexts = null,
            IGameTableManager questTables = null, IContractManager contracts = null, CharacterDatabase characterDatabase = null,
            IGameTableManager rewardTables = null, Func<AuthContext> authContexts = null, AuthDatabase authDatabase = null,
            CharacterModel actorModel = null, IAccount actorAccount = null, IGameSession actorSession = null,
            EventQueue actorEvents = null, Func<Player, CharacterModel, IInventory> inventoryFactory = null)
        {
            f = fixture; _ = Manager(f.Source); // installs reviewed tier goals
            Events = actorEvents ?? new();
            Events.UnhandledExceptionHandler = Errors.Add;
            Player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
            foreach (var property in typeof(Player).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.PropertyType.IsInterface && p.Name.EndsWith("Manager", StringComparison.Ordinal)))
            {
                var args = new object[] { null }; var value = typeof(RecordingDispatchProxy<>).MakeGenericType(property.PropertyType).GetMethod("Create")!.Invoke(null, args); Set(property.Name, value);
            }
            Set(nameof(Player.Identity), new Identity { Id = actorModel?.Id ?? 42, RealmId = 1 });
            Set(nameof(Player.Session), actorSession ?? f.Source.Player.Session); Set(nameof(Player.Account), actorAccount ?? f.Source.Player.Account);
            Set(nameof(Player.Inventory), inventoryFactory?.Invoke(Player, actorModel) ?? f.Source.Inventory);
            if (actorSession == null) f.Source.SessionProxy.SetProperty("Events", Events);
            else if (!ReferenceEquals(actorSession.Events, Events)) throw new InvalidOperationException("Test actor must own its supplied queue.");
            foreach (string name in new[] { "tradeskills", "schematics", "taxiNodes", "unsavedTaxiNodes" })
            { var field = typeof(Player).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!; field.SetValue(Player, Activator.CreateInstance(field.FieldType)); }
            foreach (string name in new[] { "taxiStateLock", "bindStateLock" }) Field(name, new object());
            Field("saveTimer", new UpdateTimer(1000));
            var pending = typeof(UnitEntity).GetField("pendingSpells", BindingFlags.NonPublic | BindingFlags.Instance)!; pending.SetValue(Player, Activator.CreateInstance(pending.FieldType));
            var cleanup = RecordingDispatchProxy<ICleanupManager>.Create(out var cleanupProxy); CleanupProxy = cleanupProxy; Field("cleanupManager", cleanup);
            var publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out var publisherProxy); PublisherProxy = publisherProxy; publisherProxy.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask); Field("messagePublisher", publisher);
            var stats = typeof(WorldEntity).GetField("stats", BindingFlags.NonPublic | BindingFlags.Instance)!; stats.SetValue(Player, Activator.CreateInstance(stats.FieldType));
            using var read = f.Context();
            // PersistSecurityInfo=false strips MySQL credentials after opening.
            // Retain the original configuration before the model queries below.
            string configuredConnection = read.Database.GetDbConnection().ConnectionString;
            Identity = ChallengeManager.CaptureRewardPersistenceIdentity(read);
            var model = actorModel ?? new CharacterModel { Id = 42 };
            if (actorModel == null)
            {
                foreach (var row in read.CharacterChallenge.AsNoTracking().Where(row => row.Id == model.Id)) model.Challenge.Add(row);
                foreach (var row in read.CharacterQuest.AsNoTracking().Where(row => row.Id == model.Id).Include(row => row.QuestObjective)) model.Quest.Add(row);
            }
            if (rewardTables != null) ((RecordingDispatchProxy<IGameTableManager>)(object)rewardTables).SetProperty(nameof(IGameTableManager.Challenge), f.Source.Tables.Challenge);
            Challenges = new(Player, model, rewardTables ?? f.Source.Tables, rewardContexts ?? (() => f.Context()), prerequisites,
                (IItemManager)(object)f.Source.ManagerProxy, authContexts); Set(nameof(Player.ChallengeManager), Challenges);
            Quests = new(Player, model, prerequisiteManager: prerequisites, globalQuestManager: f.Global, scriptManager: f.Scripts, gameTableManager: questTables ?? f.Source.Tables,
                contractManager: contracts); Set(nameof(Player.QuestManager), Quests);
            var config = RecordingDispatchProxy<IConnectionString>.Create(out var cp); cp.SetProperty(nameof(IConnectionString.Provider), read.Database.IsSqlite() ? DatabaseProvider.Sqlite : DatabaseProvider.MySql); cp.SetProperty(nameof(IConnectionString.ConnectionString), configuredConnection);
            var auth = authDatabase ?? new AuthDatabase(); if (authDatabase == null) auth.Initialise(config);
            var character = characterDatabase ?? new CharacterDatabase(); character.Initialise(config);
            var databases = RecordingDispatchProxy<IDatabaseManager>.Create(out var dp); dp.SetMethodHandler(nameof(IDatabaseManager.GetDatabase), (method, _) => method.GetGenericArguments()[0] == typeof(AuthDatabase) ? auth : character); Field("databaseManager", databases);
            ((RecordingDispatchProxy<IAccount>)(object)Player.Account).SetMethodHandler(nameof(IAccount.Save), _ => { OnAuth?.Invoke(); return null; });
        }
        internal void Set(string property, object value)
        {
            var info = typeof(Player).GetProperty(property)!;
            if (info.SetMethod != null) info.SetValue(Player, value);
            else
            {
                var backing = info.DeclaringType!.GetField("<" + property + ">k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                if (backing != null) backing.SetValue(Player, value);
            }
        }
        private void Field(string name, object value) => typeof(Player).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(Player, value);
        internal async Task Load() { Challenges.SendInitialPackets(); await Drain(); Assert.NotNull(Challenges.NormalSaveIdentity); }
        internal void StartSave() => typeof(Player).GetMethod("StartSave", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(Player, [null, null]);
        internal void Cleanup() => typeof(Player).GetMethod("Cleanup", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(Player, null);
        internal QuestManager.QuestRewardCapture Complete(Action action = null)
        {
            Assert.True(Quests.TryCaptureRewardCompletion(Identity.ExecutionDatabase, action ?? (() => Quests.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1)), out var capture)); return capture;
        }
        internal bool QueueTerminal(QuestManager.QuestRewardCapture capture, uint track = 16)
        {
            var queue = (ChallengeManager.CharacterRewardQueue)typeof(ChallengeManager).GetField("rewardQueue", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Challenges)!;
            return queue.Enqueue([Challenges.CaptureRewardIntent(f.Source.Challenge(track))], held =>
            {
                var before = held.Expected(f.Source.Challenge(track)); return CharacterChallengeRewardCommand.Credit(before,
                    before with { Activated = false, ActiveSeconds = 0, Tier = 2, LastRewardTier = 2, Completions = before.Completions + 1 }, 2, new(1, 3), DateTime.UtcNow);
            }, quests: capture);
        }
        internal async Task Drain(bool allowErrors = false)
        {
            var timeout = Stopwatch.StartNew();
            do
            {
                Events.Update(.01);
                if (timeout.Elapsed >= TimeSpan.FromSeconds(20))
                    Console.Error.WriteLine(DrainTimeoutState(timeout.Elapsed));
                Assert.True(timeout.Elapsed < TimeSpan.FromSeconds(20), "Normal-save/reward work did not settle.");
                await Task.Delay(5);
            }
            while (Challenges.HasPendingRewardOperation || Player.IsNormalSaveInProgress || Events.PendingEvents);
            if (!allowErrors) Assert.Empty(Errors);
        }
        private string DrainTimeoutState(TimeSpan elapsed)
        {
            // Failure-only, non-atomic observations: do not acquire the potentially
            // blocked persistence gate or include database/character contents.
            try
            {
                static object Read(object owner, string name) => owner?.GetType()
                    .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(owner);
                static string State(object value) => value switch
                {
                    null => "null",
                    Task task => task.Status.ToString(),
                    bool flag => flag.ToString(),
                    int count => count.ToString(),
                    _ => "present"
                };
                object queue = Read(Challenges, "rewardQueue");
                object pending = Read(queue, "pending");
                var observations = new List<string>
                {
                    $"elapsed={elapsed.TotalSeconds:F3}",
                    $"normalSave={Player.IsNormalSaveInProgress}",
                    $"events={Events.PendingEvents}",
                    $"pending={pending?.GetType().GetProperty("Count")?.GetValue(pending)}"
                };
                foreach (string name in new[] { "current", "executing", "preparingIdentity", "stopped", "invalidated", "finalized", "probing", "recoveries", "lease", "questPreparation" })
                    observations.Add($"queue.{name}={State(Read(queue, name))}");
                foreach (string name in new[] { "rewardQueueRequests", "activeRewardLease", "activeSave" })
                    observations.Add($"manager.{name}={State(Read(Challenges, name))}");
                ThreadPool.GetAvailableThreads(out int availableWorkers, out int availableIo);
                ThreadPool.GetMinThreads(out int minimumWorkers, out int minimumIo);
                ThreadPool.GetMaxThreads(out int maximumWorkers, out int maximumIo);
                observations.Add($"pool.threads={ThreadPool.ThreadCount};pending={ThreadPool.PendingWorkItemCount};workers={availableWorkers}/{minimumWorkers}/{maximumWorkers};io={availableIo}/{minimumIo}/{maximumIo};processors={Environment.ProcessorCount}");
                observations.Add("fixtureConnections=" + f.Source.DescribeConnectionDiagnostics());
                return "Drain timeout (non-atomic): " + string.Join(";", observations);
            }
            catch (Exception exception)
            {
                return "Drain timeout diagnostic failed: " + exception.GetType().Name;
            }
        }
        public void Dispose()
        {
            if (typeof(Player).GetField("normalRewardSave", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Player) is Player.NormalRewardSave save) save.Finish(false);
            Challenges.InvalidateRewardOperations(true); Quests.Dispose();
        }
    }
    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => after ? new(result) : throw new IOException("Frozen normal save before commit.");
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => after ? Task.FromException(new IOException("Frozen normal save acknowledgement lost.")) : Task.CompletedTask;
    }
}
