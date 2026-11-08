using System.Data.Common;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.PublicEvent;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model.PublicEvent;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Shared.Game.Events;
using NexusForever.Script;
using NexusForever.Script.Instance;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Collection;
using NexusForever.WorldServer.Network;
using EventTeam = NexusForever.Game.Static.PublicEvent.PublicEventTeam;
using RuntimeEvent = NexusForever.Game.PublicEvent.PublicEvent;
using RuntimeTeam = NexusForever.Game.PublicEvent.PublicEventTeam;
using SaveActor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using SaveFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;
using Xunit.Abstractions;

namespace NexusForever.Game.Tests.PublicEvents;

public sealed class PublicEventCompletionOwnershipTests(ITestOutputHelper output)
{
    private readonly DbContextOptions<CharacterContext> options;

    internal PublicEventCompletionOwnershipTests(ITestOutputHelper output, DbContextOptions<CharacterContext> options)
        : this(output)
    {
        this.options = options;
    }

    [Fact]
    public void MapWideObjectiveUpdateStopsWhenTheFirstTeamFinishesTheEvent()
    {
        const uint objectId = 501u;
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        var red = new Actor(map, 42ul, 100u);
        var blue = new Actor(map, 43ul, 200u);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] == 42ul ? red.Player : blue.Player);
        var publicEvent = CreateEvent(players, map,
            CreateObjective(201u, objectId, EventTeam.RedTeam),
            CreateObjective(202u, objectId, EventTeam.BlueTeam));
        publicEvent.JoinEvent(red.Player, EventTeam.RedTeam);
        publicEvent.JoinEvent(blue.Player, EventTeam.BlueTeam);
        var losingObjective = publicEvent.GetTeams().Single(team => team.Team == EventTeam.BlueTeam).GetObjectives().Single();

        publicEvent.UpdateObjective(PublicEventObjectiveType.Script, objectId, 1);

        Assert.True(publicEvent.IsFinalised);
        Assert.Equal(EventTeam.RedTeam, Assert.IsAssignableFrom<IPublicEventTeam>(
            Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.OnPublicEventFinish))).Arguments[1]).Team);
        AssertCredit(red, 201u);
        Assert.Equal(PublicEventRemoveReason.Failure, Assert.Single(blue.Packets.OfType<ServerPublicEventEnd>()).Reason);
        Assert.Equal(PublicEventStatus.Active, losingObjective.Status);
        Assert.Equal(0u, losingObjective.Count);
    }

    // The client keeps the reason of the last ServerPublicEventEnd it received per event (native FUN_1405f30a0 stores it at event +0x1d0, read by prerequisite 199).
    [Fact]
    public void EndDeliveredToAPlayerIsRecordedAsTheirLastEndReasonForThatEvent()
    {
        const uint objectId = 501u;
        var map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        var red = new Actor(map, 42ul, 100u);
        var blue = new Actor(map, 43ul, 200u);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] == 42ul ? red.Player : blue.Player);
        var publicEvent = CreateEvent(players, map,
            CreateObjective(201u, objectId, EventTeam.RedTeam),
            CreateObjective(202u, objectId, EventTeam.BlueTeam));
        publicEvent.JoinEvent(red.Player, EventTeam.RedTeam);
        publicEvent.JoinEvent(blue.Player, EventTeam.BlueTeam);

        publicEvent.UpdateObjective(PublicEventObjectiveType.Script, objectId, 1);

        var redCall = Assert.Single(red.Proxy.GetInvocations(nameof(IPlayer.RecordPublicEventEnd)));
        Assert.Equal(publicEvent.Id, redCall.Arguments[0]);
        Assert.Equal(PublicEventRemoveReason.Success, redCall.Arguments[1]);
        var blueCall = Assert.Single(blue.Proxy.GetInvocations(nameof(IPlayer.RecordPublicEventEnd)));
        Assert.Equal(publicEvent.Id, blueCall.Arguments[0]);
        Assert.Equal(PublicEventRemoveReason.Failure, blueCall.Arguments[1]);
    }

    [Fact]
    public void ObjectiveBatchStopsWhenAnObjectiveStatusScriptFinishesItsEvent()
    {
        const uint objectId = 501u;
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out _);
        var scripts = RecordingDispatchProxy<IScriptManager>.Create(out var manager);
        var collection = RecordingDispatchProxy<IScriptCollection>.Create(out var callbacks);
        manager.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), collection);
        var script = new FinishOnObjectiveSuccess();
        callbacks.SetMethodHandler(nameof(IScriptCollection.Invoke), args =>
        {
            if (args[0] is Action<IPublicEventScript> invoke)
                invoke(script);
            return null;
        });
        var publicEvent = CreateEvent(players, map, scripts,
            CreateObjective(201u, objectId, EventTeam.PublicTeam),
            CreateObjective(202u, objectId, EventTeam.PublicTeam));
        var objectives = publicEvent.GetTeams().Single().GetObjectives().ToArray();

        publicEvent.UpdateObjective(PublicEventObjectiveType.Script, objectId, 1);

        Assert.True(publicEvent.IsFinalised);
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.OnPublicEventFinish)));
        Assert.Equal(PublicEventStatus.Succeeded, objectives[0].Status);
        Assert.Equal(PublicEventStatus.Active, objectives[1].Status);
        Assert.Equal(0u, objectives[1].Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EndingEventRejectsScriptObjectiveAndPhaseChanges(bool duringNotice)
    {
        var map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        var actor = new Actor(map, 42ul, 100u);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodReturn(nameof(IPlayerManager.GetPlayer), actor.Player);
        var publicEvent = CreateEvent(players, map, 201u, 501u);
        publicEvent.SetPhase(7u);
        publicEvent.JoinEvent(actor.Player, EventTeam.PublicTeam);
        var objective = Assert.Single(Assert.Single(publicEvent.GetTeams()).GetObjectives());

        void ApplyLateProgress()
        {
            publicEvent.UpdateObjective(201u, 1);
            publicEvent.ResetObjective(201u);
            publicEvent.ActivateObjective(201u, 3);
            publicEvent.SetPhase(9u);
        }

        if (duringNotice)
            actor.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
            {
                if (args[0] is ServerPublicEventEnd)
                    ApplyLateProgress();
                return null;
            });
        publicEvent.Finish(null);
        if (!duringNotice)
            ApplyLateProgress();

        Assert.True(publicEvent.IsFinalised);
        Assert.Equal(7u, publicEvent.Phase);
        Assert.Equal(PublicEventStatus.Active, objective.Status);
        Assert.Equal(0u, objective.Count);
        Assert.Equal(0u, objective.DynamicMax);
        Assert.Empty(actor.Achievements.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        Assert.Empty(actor.Quests.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Single(actor.Packets.OfType<ServerPublicEventEnd>());
    }

    [Theory]
    [InlineData("current")]
    [InlineData("replacement")]
    [InlineData("unregistered")]
    [InlineData("moved")]
    [InlineData("rebound")]
    public void CompletionUsesTheOriginalActiveMembership(string scenario)
    {
        const uint objectiveId = 201u;
        const uint objectId = 501u;
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        var original = new Actor(map, 42ul, 100u);
        var replacement = new Actor(map, 42ul, 200u);
        var teammate = new Actor(map, 43ul, 300u);
        IPlayer registered = original.Player;
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var playerManager);
        playerManager.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] == 43ul ? teammate.Player : registered);
        var publicEvent = CreateEvent(players, map, objectiveId, objectId);
        publicEvent.JoinEvent(original.Player, EventTeam.PublicTeam);
        publicEvent.JoinEvent(teammate.Player, EventTeam.PublicTeam);

        switch (scenario)
        {
            case "replacement": registered = replacement.Player; break;
            case "unregistered": registered = null; break;
            case "moved": original.Proxy.SetProperty(nameof(IPlayer.Map), RecordingDispatchProxy<IBaseMap>.Create(out _)); break;
            case "rebound": original.Session.SetProperty(nameof(IWorldSession.Player), replacement.Player); break;
        }

        IPlayer contributor = scenario == "replacement" ? replacement.Player : original.Player;
        publicEvent.UpdateObjective(contributor, PublicEventObjectiveType.Script, objectId, 1);
        if (scenario != "current")
            Assert.False(publicEvent.IsFinalised);
        publicEvent.UpdateObjective(objectiveId, 1);

        Assert.True(publicEvent.IsFinalised);
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.OnPublicEventFinish)));
        AssertCredit(teammate, objectiveId);
        var scoreboard = Assert.Single(teammate.Packets.OfType<ServerPublicEventEnd>());
        var enrolledRow = Assert.Single(scoreboard.ParticipantStats, row => row.Player.Id == 42ul);
        Assert.Equal(100u, enrolledRow.UnitId);
        AssertNoCredit(replacement);
        if (scenario == "current")
            AssertCredit(original, objectiveId);
        else
            AssertNoCredit(original);
    }

    [Theory]
    [InlineData(typeof(ServerPublicEventObjectiveStatusUpdate))]
    [InlineData(typeof(ServerPublicEventEnd))]
    public void RejectedMemberNoticeDoesNotInterruptCompletion(Type rejectedPacket)
    {
        const uint objectiveId = 201u;
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        var first = new Actor(map, 42ul, 100u);
        var teammate = new Actor(map, 43ul, 200u);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] == 42ul ? first.Player : teammate.Player);
        var publicEvent = CreateEvent(players, map, objectiveId, 501u);
        publicEvent.JoinEvent(first.Player, EventTeam.PublicTeam);
        publicEvent.JoinEvent(teammate.Player, EventTeam.PublicTeam);
        int rejections = 0;
        first.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0].GetType() == rejectedPacket)
            {
                rejections++;
                throw new IOException("Owned public-event notice rejection.");
            }
            return null;
        });

        publicEvent.UpdateObjective(objectiveId, 1);
        Assert.True(publicEvent.IsFinalised);
        publicEvent.Finish(EventTeam.PublicTeam);

        Assert.Equal(1, rejections);
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.OnPublicEventFinish)));
        Assert.Empty(publicEvent.GetTeams().SelectMany(team => team.GetMembers()));
        AssertCredit(first, objectiveId);
        AssertCredit(teammate, objectiveId);
    }

    [Fact]
    public void RejectedLeaveNoticeDoesNotKeepAnEmptyEventRunning()
    {
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        var actor = new Actor(map, 42ul, 100u);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodReturn(nameof(IPlayerManager.GetPlayer), actor.Player);
        var publicEvent = CreateEvent(players, map, 201u, 501u);
        publicEvent.JoinEvent(actor.Player, EventTeam.PublicTeam);
        actor.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerPublicEventLeave)
                throw new IOException("Owned public-event leave notice rejection.");
            return null;
        });

        publicEvent.LeaveEvent(actor.Player, PublicEventRemoveReason.Failure);

        Assert.True(publicEvent.IsFinalised);
        Assert.Single(actor.Packets.OfType<ServerPublicEventLeave>());
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.OnPublicEventFinish)));
        Assert.Empty(publicEvent.GetTeams().SelectMany(team => team.GetMembers()));
    }

    [Fact]
    public void RejectedStartNoticeDoesNotInterruptPartyEnrollment()
    {
        var map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        var first = new Actor(map, 42ul, 100u);
        var teammate = new Actor(map, 43ul, 200u);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] == 42ul ? first.Player : teammate.Player);
        var publicEvent = CreateEvent(players, map, 201u, 501u);
        first.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerPublicEventStart)
                throw new IOException("Owned public-event start notice rejection.");
            return null;
        });

        publicEvent.JoinEvent(first.Player, EventTeam.PublicTeam);
        publicEvent.JoinEvent(teammate.Player, EventTeam.PublicTeam);

        Assert.Single(first.Packets.OfType<ServerPublicEventStart>());
        Assert.Single(teammate.Packets.OfType<ServerPublicEventStart>());
        Assert.Equal(new[] { first.Player, teammate.Player }, Assert.Single(publicEvent.GetTeams()).GetMembers()
            .Select(member => member.GetCurrentPlayer()));
    }

    [Fact]
    public void FailedStartRetriesBeforeRegisteredObjectiveUpdatesOnItsOriginalQueue()
    {
        var (publicEvent, actor, teammate) = CreateStartParty();
        var sent = new List<IWritable>();
        int attempts = 0;
        actor.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerPublicEventStart && ++attempts == 1)
                throw new IOException("Start failed before enqueue.");
            sent.Add((IWritable)args[0]);
            return null;
        });

        publicEvent.JoinEvent(actor.Player, EventTeam.PublicTeam);
        publicEvent.JoinEvent(teammate.Player, EventTeam.PublicTeam);
        publicEvent.UpdateObjective(201u, 1);
        Assert.Empty(sent);
        actor.Events.Update(0);

        Assert.Equal(2, attempts);
        Assert.Collection(sent,
            message => Assert.Equal(0u, Assert.Single(Assert.IsType<ServerPublicEventStart>(message).Objectives).ObjectiveStatus.Count),
            message => Assert.Equal(1u, Assert.IsType<ServerPublicEventObjectiveUpdate>(message).Objective.ObjectiveStatus.Count));
        var messages = new MessageManager(NullLogger<MessageManager>.Instance);
        messages.RegisterMessage(typeof(ServerPublicEventObjectiveUpdate));
        Assert.Equal(GameMessageOpcode.ServerPublicEventObjectiveUpdate, messages.GetOpcode(sent[1]));
        var packet = new ServerGamePacket(messages.GetOpcode(sent[1]).Value, sent[1]);
        using var reader = new GamePacketReader(new MemoryStream(packet.Data));
        Assert.Equal(201u, reader.ReadUInt(15));
        Assert.Single(teammate.Packets.OfType<ServerPublicEventObjectiveUpdate>());
        Assert.False(actor.Events.PendingEvents);
    }

    [Theory]
    [InlineData("finish")]
    [InlineData("leave")]
    [InlineData("rejoin")]
    [InlineData("queue")]
    [InlineData("player")]
    public void AStartRetryCannotReviveAnEndedOrReplacedMembership(string scenario)
    {
        var (publicEvent, actor, teammate) = CreateStartParty();
        int attempts = 0;
        actor.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerPublicEventStart && ++attempts == 1)
                throw new IOException("Start failed before enqueue.");
            return null;
        });
        publicEvent.JoinEvent(actor.Player, EventTeam.PublicTeam);
        publicEvent.JoinEvent(teammate.Player, EventTeam.PublicTeam);
        if (scenario == "finish") publicEvent.Finish(null);
        if (scenario is "leave" or "rejoin") publicEvent.LeaveEvent(actor.Player, PublicEventRemoveReason.Failure);
        if (scenario == "rejoin") publicEvent.JoinEvent(actor.Player, EventTeam.PublicTeam);
        if (scenario == "queue") actor.Session.SetProperty(nameof(IGameSession.Events), new EventQueue());
        if (scenario == "player") actor.Session.SetProperty(nameof(IWorldSession.Player), teammate.Player);

        actor.Events.Update(0);

        Assert.Equal(scenario == "rejoin" ? 2 : 1, attempts);
        Assert.False(actor.Events.PendingEvents);
    }

    [Fact]
    public void PersistentStartFailureStopsAfterOneRetryAndKeepsOtherMembersReceivingUpdates()
    {
        var (publicEvent, actor, teammate) = CreateStartParty();
        int attempts = 0;
        var sent = new List<IWritable>();
        actor.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerPublicEventStart)
            {
                attempts++;
                throw new IOException("Persistent start failure before enqueue.");
            }
            sent.Add((IWritable)args[0]);
            return null;
        });
        publicEvent.JoinEvent(actor.Player, EventTeam.PublicTeam);
        publicEvent.JoinEvent(teammate.Player, EventTeam.PublicTeam);
        publicEvent.UpdateObjective(201u, 1);
        actor.Events.Update(0);
        publicEvent.UpdateObjective(201u, 1);
        actor.Events.Update(0);

        Assert.Equal(2, attempts);
        Assert.Empty(sent);
        Assert.Equal(2, teammate.Packets.OfType<ServerPublicEventObjectiveUpdate>().Count());
        publicEvent.Finish(null);
        Assert.Single(sent.OfType<ServerPublicEventEnd>());
        Assert.False(actor.Events.PendingEvents);
    }

    [Fact]
    public void NoticesProducedDuringTheStartRetryDrainRemainInOrder()
    {
        var (publicEvent, actor, _) = CreateStartParty();
        int starts = 0;
        bool advanced = false;
        var sent = new List<IWritable>();
        actor.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is ServerPublicEventStart && ++starts == 1)
                throw new IOException("Start failed before enqueue.");
            if (args[0] is ServerPublicEventStart)
                publicEvent.UpdateObjective(201u, 1);
            else if (args[0] is ServerPublicEventObjectiveUpdate && !advanced)
            {
                advanced = true;
                publicEvent.UpdateObjective(201u, 1);
            }
            sent.Add((IWritable)args[0]);
            return null;
        });
        publicEvent.JoinEvent(actor.Player, EventTeam.PublicTeam);

        actor.Events.Update(0);

        Assert.Collection(sent,
            message => Assert.IsType<ServerPublicEventStart>(message),
            message => Assert.Equal(1u, Assert.IsType<ServerPublicEventObjectiveUpdate>(message).Objective.ObjectiveStatus.Count),
            message => Assert.Equal(2u, Assert.IsType<ServerPublicEventObjectiveUpdate>(message).Objective.ObjectiveStatus.Count));
        Assert.False(actor.Events.PendingEvents);
    }

    private static (RuntimeEvent Event, Actor Actor, Actor Teammate) CreateStartParty()
    {
        var map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        var actor = new Actor(map, 42ul, 100u);
        var teammate = new Actor(map, 43ul, 200u);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args => (ulong)args[0] == 42ul ? actor.Player : teammate.Player);
        var objective = CreateObjective(201u, 501u, EventTeam.PublicTeam);
        objective.Count = 3;
        return (CreateEvent(players, map, objective), actor, teammate);
    }

    [Fact]
    public void ObjectiveCompletionNotifiesTheMapOnceWhenMatchFinishCallsBack()
    {
        var (script, publicEvent, actor, map) = CreateContentEvent();
        var match = RecordingDispatchProxy<IMatch>.Create(out var matchProxy);
        map.SetProperty(nameof(IContentMapInstance.Match), match);
        bool matchFinished = false;
        matchProxy.SetMethodHandler(nameof(IMatch.MatchFinish), _ =>
        {
            // MatchFinish sets its status before dispatching OnMatchFinish.
            if (!matchFinished)
            {
                matchFinished = true;
                script.OnMatchFinish();
            }
            return null;
        });

        publicEvent.UpdateObjective(201u, 1);

        Assert.True(publicEvent.IsFinalised);
        Assert.Single(map.GetInvocations(nameof(IBaseMap.OnPublicEventFinish)));
        Assert.Single(matchProxy.GetInvocations(nameof(IMatch.MatchFinish)));
        AssertCredit(actor, 201u);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ContentMapDoesNotJoinArrivalsToAnEndingEvent(bool arrivesDuringEndNotice)
    {
        var (script, publicEvent, actor, _) = CreateContentEvent();
        var late = new Actor(publicEvent.Map, 43ul, 200u);
        if (arrivesDuringEndNotice)
            actor.Session.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
            {
                if (args[0] is ServerPublicEventEnd)
                    script.OnAddToMap(late.Player);
                return null;
            });

        publicEvent.UpdateObjective(201u, 1);
        if (!arrivesDuringEndNotice)
            script.OnAddToMap(late.Player);

        Assert.True(publicEvent.IsFinalised);
        Assert.Empty(late.Packets.OfType<ServerPublicEventStart>());
        Assert.Empty(publicEvent.GetTeams().SelectMany(team => team.GetMembers()));
    }

    [Theory]
    [InlineData("direct-rollback")]
    [InlineData("unflushed-commit")]
    [InlineData("queued-rollback")]
    [InlineData("queued-success")]
    [InlineData("queued-retry")]
    [InlineData("queued-lost-ack")]
    [InlineData("queued-owner-replaced")]
    [InlineData("replaced-after-commit")]
    public async Task PublicEventFinalityAndQuestCreditAreSeparateFromCharacterRollback(string outcome)
    {
        const uint objectiveId = 201u;
        using var f = new SaveFixture(options, new QuestObjectiveEntry
        {
            Id = 90010, Type = (uint)QuestObjectiveType.CompletePublicEventObjective, Data = objectiveId, Count = 2
        });
        f.Seed();
        bool useWorldSaveQueue = outcome.StartsWith("queued-", StringComparison.Ordinal);
        bool reject = outcome.EndsWith("-rollback", StringComparison.Ordinal);
        bool replaceOwner = outcome == "queued-owner-replaced";
        var database = new PublicEventSaveDatabase(f, outcome);
        using var actor = new SaveActor(f, characterDatabase: database);
        if (useWorldSaveQueue)
            await actor.Load();
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        actor.Set(nameof(IPlayer.Map), map);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodReturn(nameof(IPlayerManager.GetPlayer), actor.Player);
        var publicEvent = CreateEvent(players, map, objectiveId, 501);
        publicEvent.JoinEvent(actor.Player, EventTeam.PublicTeam);
        if (reject)
            using (var setup = f.Context())
                await setup.Database.ExecuteSqlRawAsync(setup.Database.IsSqlite() ? """
                    CREATE TRIGGER reject_public_event_quest BEFORE UPDATE ON character_quest_objective
                    BEGIN SELECT RAISE(ABORT, 'owned public event quest write failure'); END
                    """ : """
                    CREATE TRIGGER reject_public_event_quest BEFORE UPDATE ON character_quest_objective
                    FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'owned public event quest write failure'
                    """);

        IEnumerable<object> Packets() => f.Source.SessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]);
        void AssertNoQuestNotices()
        {
            Assert.Empty(Packets().OfType<ServerQuestObjectiveUpdate>());
            Assert.Empty(Packets().OfType<ServerQuestStateChange>());
            Assert.Empty(Packets().OfType<ServerQuestObjectiveWorldLocation>());
        }
        int authSaves = 0;
        actor.OnAuth = () => authSaves++;
        database.BeforeRetry = () =>
        {
            AssertNoQuestNotices();
            using var read = f.Context();
            Assert.Equal(1u, read.CharacterQuestObjective.Single().Progress);
            Assert.Empty(read.BankReconciliationReceipt);
        };
        if (replaceOwner)
            database.OnRollback = () =>
            {
                var replacement = RecordingDispatchProxy<IWorldSession>.Create(out _);
                actor.Set(nameof(IPlayer.Session), replacement);
            };

        publicEvent.UpdateObjective(objectiveId, 1);
        Assert.True(publicEvent.IsFinalised);
        Assert.Single(Packets().OfType<ServerPublicEventEnd>());
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.OnPublicEventFinish)));
        Assert.Equal(QuestState.Achieved, actor.Quest.State);
        AssertNoQuestNotices();

        Task Save() => CharacterAtomicSave.Execute(() => f.Context(), actor.Player.CharacterId, actor.Player.Save);
        if (useWorldSaveQueue)
        {
            actor.StartSave();
            await actor.Drain();
            Assert.Equal(1, authSaves);
            Assert.Equal(outcome is "queued-retry" or "queued-rollback" ? 2 : 1, database.Attempts);
            output.WriteLine("World save disconnect requests: {0}. Quest reload required: {1}.",
                f.Source.SessionProxy.GetInvocations(nameof(IWorldSession.ForceDisconnect)).Count,
                actor.Quests.RequiresAuthoritativeReload);
        }
        else if (reject)
            await Assert.ThrowsAsync<DbUpdateException>(Save);
        else if (outcome == "unflushed-commit")
        {
            await using (var context = f.Context())
            await using (var transaction = await context.Database.BeginTransactionAsync())
            {
                actor.Player.Save(context);
                await transaction.CommitAsync();
            }
            actor.Events.Update(.01);
            AssertNoQuestNotices();
            using (var unflushed = f.Context())
                Assert.Equal(1u, unflushed.CharacterQuestObjective.Single().Progress);
            await Save();
        }
        else
            await Save();
        if (reject || replaceOwner)
        {
            actor.Events.Update(.01);
            AssertNoQuestNotices();
            using (var failed = f.Context())
            {
                Assert.Equal((byte)QuestState.Accepted, failed.CharacterQuest.Single().State);
                Assert.Equal(1u, failed.CharacterQuestObjective.Single().Progress);
                Assert.Empty(failed.BankReconciliationReceipt);
                if (reject)
                    await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_public_event_quest");
            }
            if (useWorldSaveQueue)
            {
                Assert.Equal(replaceOwner ? 0 : 1,
                    f.Source.SessionProxy.GetInvocations(nameof(IWorldSession.ForceDisconnect)).Count);
                Assert.True(actor.Quests.RequiresAuthoritativeReload);
                return;
            }
            await Save();
        }

        using (var committed = f.Context())
        {
            Assert.Equal((byte)QuestState.Achieved, committed.CharacterQuest.Single().State);
            Assert.Equal(2u, committed.CharacterQuestObjective.Single().Progress);
            if (useWorldSaveQueue)
            {
                using var payload = JsonDocument.Parse(Assert.Single(committed.BankReconciliationReceipt).Payload);
                Assert.Equal("normal-public-event-quest", payload.RootElement.GetProperty("Kind").GetString());
                Assert.Empty(f.Source.SessionProxy.GetInvocations(nameof(IWorldSession.ForceDisconnect)));
                Assert.False(actor.Quests.RequiresAuthoritativeReload);
            }
        }
        if (outcome == "replaced-after-commit")
        {
            var replacement = RecordingDispatchProxy<IWorldSession>.Create(out var replacementNetwork);
            actor.Set(nameof(IPlayer.Session), replacement);
            actor.Events.Update(.01);
            AssertNoQuestNotices();
            Assert.Empty(replacementNetwork.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        }
        else
        {
            actor.Events.Update(.01);
            Assert.Equal(2u, Assert.Single(Packets().OfType<ServerQuestObjectiveUpdate>()).Completed);
            Assert.Equal(QuestState.Achieved, Assert.Single(Packets().OfType<ServerQuestStateChange>()).QuestState);
            Assert.Single(Packets().OfType<ServerQuestObjectiveWorldLocation>());
        }
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.OnPublicEventFinish)));
    }

    [Theory]
    [InlineData("objective")]
    [InlineData("state")]
    [InlineData("replaced-queue")]
    [InlineData("persistent")]
    public async Task CommittedQuestNoticesResumeFromTheRejectedEnqueue(string outcome)
    {
        const uint objectiveId = 201u;
        using var f = new SaveFixture(options, new QuestObjectiveEntry
        {
            Id = 90010, Type = (uint)QuestObjectiveType.CompletePublicEventObjective, Data = objectiveId, Count = 2
        });
        f.Seed();
        var database = new PublicEventSaveDatabase(f, "queued-success");
        using var actor = new SaveActor(f, characterDatabase: database);
        await actor.Load();
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        actor.Set(nameof(IPlayer.Map), map);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodReturn(nameof(IPlayerManager.GetPlayer), actor.Player);
        var publicEvent = CreateEvent(players, map, objectiveId, 501);
        publicEvent.JoinEvent(actor.Player, EventTeam.PublicTeam);

        var rejectedType = outcome == "state" ? typeof(ServerQuestStateChange) : typeof(ServerQuestObjectiveUpdate);
        var attempts = new List<Type>();
        var accepted = new List<Type>();
        int rejections = 0;
        var replacementQueue = new NexusForever.Shared.Game.Events.EventQueue();
        f.Source.SessionProxy.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted), args =>
        {
            if (args[0] is not (ServerQuestObjectiveUpdate or ServerQuestStateChange or ServerQuestObjectiveWorldLocation))
                return null;

            var type = args[0].GetType();
            attempts.Add(type);
            if (type == rejectedType && (rejections == 0 || outcome == "persistent"))
            {
                rejections++;
                if (outcome == "replaced-queue")
                    f.Source.SessionProxy.SetProperty(nameof(IWorldSession.Events), replacementQueue);
                throw new IOException("Owned public-event quest packet rejection.");
            }
            accepted.Add(type);
            return null;
        });

        publicEvent.UpdateObjective(objectiveId, 1);
        Assert.True(publicEvent.IsFinalised);
        Assert.Empty(attempts);
        actor.StartSave();
        await actor.Drain();

        Assert.Equal(1, database.Attempts);
        Assert.Single(mapProxy.GetInvocations(nameof(IBaseMap.OnPublicEventFinish)));
        using var read = f.Context();
        Assert.Equal(2u, read.CharacterQuestObjective.Single().Progress);
        Assert.Equal((byte)QuestState.Achieved, read.CharacterQuest.Single().State);
        if (outcome is "persistent" or "replaced-queue")
        {
            Assert.Equal(outcome == "persistent" ? new[] { rejectedType, rejectedType } : [rejectedType], attempts);
            Assert.Empty(accepted);
            Assert.False(replacementQueue.PendingEvents);
            return;
        }

        Type[] expected = [typeof(ServerQuestObjectiveUpdate), typeof(ServerQuestStateChange), typeof(ServerQuestObjectiveWorldLocation)];
        Assert.Equal(expected, accepted);
        Assert.Equal(outcome == "state" ? [expected[0], expected[1], expected[1], expected[2]]
            : new[] { expected[0], expected[0], expected[1], expected[2] }, attempts);
    }

    private sealed class PublicEventSaveDatabase(SaveFixture fixture, string outcome) : CharacterDatabase
    {
        internal int Attempts { get; private set; }
        internal Action BeforeRetry { get; set; }
        internal Action OnRollback { get; set; }

        public override async Task SavePublicEventQuestReconciliation(ulong characterId, Action<CharacterContext> action,
            PublicEventQuestSaveReconciliation receipt)
        {
            Attempts++;
            if (Attempts == 2)
                BeforeRetry?.Invoke();
            bool fault = Attempts == 1 && outcome is "queued-retry" or "queued-lost-ack" or "queued-owner-replaced";
            try
            {
                await CharacterAtomicSave.ExecutePublicEventQuestReconciliation(
                    () => fault ? fixture.Context(new CommitFault(outcome == "queued-lost-ack")) : fixture.Context(),
                    characterId, action, receipt);
            }
            catch (CharacterReconciliationRolledBackException)
            {
                OnRollback?.Invoke();
                throw;
            }
        }
    }

    private sealed class CommitFault(bool afterCommit) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => afterCommit ? new(result) : throw new IOException("Owned public event failure before commit.");

        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
            => afterCommit ? Task.FromException(new IOException("Owned public event acknowledgement loss.")) : Task.CompletedTask;
    }

    private sealed class ContentEventScript(uint eventId) : EventBaseContentMapScript
    {
        public override uint PublicEventId => eventId;
    }

    private sealed class FinishOnObjectiveSuccess : IPublicEventScript
    {
        public void OnPublicEventObjectiveStatus(IPublicEventObjective objective)
        {
            if (objective.Status == PublicEventStatus.Succeeded)
                objective.Team.PublicEvent.Finish(objective.Team.Team);
        }
    }

    private static (ContentEventScript, RuntimeEvent, Actor, RecordingDispatchProxy<IContentMapInstance>) CreateContentEvent()
    {
        var map = RecordingDispatchProxy<IContentMapInstance>.Create(out var mapProxy);
        var actor = new Actor(map, 42ul, 100u);
        var players = RecordingDispatchProxy<IPlayerManager>.Create(out var registry);
        registry.SetMethodReturn(nameof(IPlayerManager.GetPlayer), actor.Player);
        var publicEvent = CreateEvent(players, map, 201u, 501u);
        var events = RecordingDispatchProxy<IPublicEventManager>.Create(out var eventsProxy);
        eventsProxy.SetMethodReturn(nameof(IPublicEventManager.CreateEvent), publicEvent);
        mapProxy.SetProperty(nameof(IContentMapInstance.PublicEventManager), events);
        var script = new ContentEventScript(publicEvent.Id);
        mapProxy.SetMethodHandler(nameof(IBaseMap.OnPublicEventFinish), args =>
        {
            script.OnPublicEventFinish((IPublicEvent)args[0], (IPublicEventTeam)args[1]);
            return null;
        });
        script.OnLoad(map);
        script.OnAddToMap(actor.Player);
        return (script, publicEvent, actor, mapProxy);
    }

    private static RuntimeEvent CreateEvent(IPlayerManager players, IBaseMap map, uint objectiveId, uint objectId)
        => CreateEvent(players, map, CreateObjective(objectiveId, objectId, EventTeam.PublicTeam));

    private static PublicEventObjectiveEntry CreateObjective(uint objectiveId, uint objectId, EventTeam team) => new()
    {
        Id = objectiveId,
        Count = 1,
        ObjectId = objectId,
        PublicEventTeamId = team,
        PublicEventObjectiveCategoryEnum = PublicEventObjectiveCategory.Main,
        PublicEventObjectiveTypeEnum = PublicEventObjectiveType.Script,
        PublicEventObjectiveFlags = PublicEventObjectiveFlag.InitialObjective
    };

    private static RuntimeEvent CreateEvent(IPlayerManager players, IBaseMap map, params PublicEventObjectiveEntry[] objectives)
        => CreateEvent(players, map, RecordingDispatchProxy<IScriptManager>.Create(out _), objectives);

    private static RuntimeEvent CreateEvent(IPlayerManager players, IBaseMap map, IScriptManager scripts,
        params PublicEventObjectiveEntry[] objectives)
    {
        var realm = RecordingDispatchProxy<IRealmContext>.Create(out var realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)1);
        var publicEvent = new RuntimeEvent(
            NullLogger<RuntimeEvent>.Instance,
            scripts,
            new PublicEventTestSupport.DelegateFactory<IPublicEventTeam>(() => new RuntimeTeam(
                NullLogger<RuntimeTeam>.Instance,
                new PublicEventTestSupport.DelegateFactory<IPublicEventObjective>(() => new PublicEventObjective()),
                new PublicEventTestSupport.DelegateFactory<IPublicEventTeamMember>(() =>
                    new PublicEventTeamMember(NullLogger<PublicEventTeamMember>.Instance, players, realm)),
                new PublicEventTestSupport.ThrowingFactory<IPublicEventVote>(),
                new PublicEventStats())),
            RecordingDispatchProxy<IPublicEventEntityFactory>.Create(out _));
        var template = new PublicEventTestSupport.TestPublicEventTemplate(objectives[0]);
        foreach (PublicEventObjectiveEntry objective in objectives.Skip(1))
        {
            template.Objectives.Add(objective.Id, objective);
            if (template.Teams.All(team => team.Id != objective.PublicEventTeamId))
                template.Teams.Add(new PublicEventTeamEntry { Id = objective.PublicEventTeamId });
        }
        publicEvent.Initialise(RecordingDispatchProxy<IPublicEventManager>.Create(out _), template, map);
        return publicEvent;
    }

    private static void AssertCredit(Actor actor, uint objectiveId)
    {
        var achievement = Assert.Single(actor.Achievements.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        Assert.Same(actor.Player, achievement.Arguments[0]);
        Assert.Equal(AchievementType.PublicEventObjectiveComplete, achievement.Arguments[1]);
        Assert.Equal(objectiveId, achievement.Arguments[2]);
        var quest = Assert.Single(actor.Quests.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)),
            call => Equals(call.Arguments[0], QuestObjectiveType.CompletePublicEventObjective));
        Assert.Equal(objectiveId, quest.Arguments[1]);
        Assert.Single(actor.Packets.OfType<ServerPublicEventObjectiveStatusUpdate>());
        Assert.Single(actor.Packets.OfType<ServerPublicEventEnd>());
    }

    private static void AssertNoCredit(Actor actor)
    {
        Assert.Empty(actor.Achievements.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
        Assert.Empty(actor.Quests.GetInvocations(nameof(IQuestManager.ObjectiveUpdate)));
        Assert.Empty(actor.Packets.OfType<ServerPublicEventObjectiveStatusUpdate>());
        Assert.Empty(actor.Packets.OfType<ServerPublicEventEnd>());
    }

    private sealed class Actor
    {
        public IPlayer Player { get; }
        public RecordingDispatchProxy<IPlayer> Proxy { get; }
        public RecordingDispatchProxy<IWorldSession> Session { get; }
        public RecordingDispatchProxy<ICharacterAchievementManager> Achievements { get; }
        public RecordingDispatchProxy<IQuestManager> Quests { get; }
        public EventQueue Events { get; } = new();
        public IEnumerable<object> Packets => Session.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
            .Select(call => call.Arguments[0]);

        public Actor(IBaseMap map, ulong characterId, uint guid)
        {
            var player = RecordingDispatchProxy<IPlayer>.Create(out var actor);
            Player = player;
            Proxy = actor;
            var session = RecordingDispatchProxy<IWorldSession>.Create(out var network);
            Session = network;
            network.SetProperty(nameof(IGameSession.Events), Events);
            network.SetProperty(nameof(IWorldSession.Player), player);
            actor.SetProperty(nameof(IPlayer.CharacterId), characterId);
            actor.SetProperty(nameof(IPlayer.Guid), guid);
            actor.SetProperty(nameof(IPlayer.Session), session);
            actor.SetProperty(nameof(IPlayer.Map), map);
            actor.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var achievements));
            Achievements = achievements;
            actor.SetProperty(nameof(IPlayer.QuestManager), RecordingDispatchProxy<IQuestManager>.Create(out var quests));
            Quests = quests;
        }
    }
}
