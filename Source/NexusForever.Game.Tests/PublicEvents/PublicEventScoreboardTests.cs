using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.PublicEvent;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Script;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Misc;
using GamePublicEventStats = NexusForever.Game.PublicEvent.PublicEventStats;
using NetworkPublicEventStats = NexusForever.Network.World.Message.Model.Shared.PublicEventStats;
using PlayerPath = NexusForever.Game.Static.PlayerPath.Path;
using RuntimePublicEventTeam = NexusForever.Game.PublicEvent.PublicEventTeam;
using PublicEventTeamId = NexusForever.Game.Static.PublicEvent.PublicEventTeam;

namespace NexusForever.Game.Tests.PublicEvents;

public class PublicEventScoreboardTests
{
    [Fact]
    public void SendScoreboardUpdate_SendsCombinedTeamAndParticipantStatsForMember()
    {
        PublicEventHarness harness = CreatePublicEventHarness();

        harness.Event.JoinEvent(harness.PlayerOne, PublicEventTeamId.PublicTeam);
        harness.Event.JoinEvent(harness.PlayerTwo, PublicEventTeamId.PublicTeam);
        harness.Event.UpdateStat(harness.PlayerOne, PublicEventStat.Damage, 10u);
        harness.Event.UpdateStat(harness.PlayerTwo, PublicEventStat.Damage, 15u);
        harness.Event.UpdateStat(harness.PlayerTwo, PublicEventStat.Kills, 2u);
        harness.Event.UpdateStat(harness.PlayerOne, PublicEventStat.Damage, 12u);

        harness.Event.SendScoreboardUpdate(harness.PlayerOne);

        ServerPublicEventStatsUpdate update = Assert.IsType<ServerPublicEventStatsUpdate>(
            harness.PlayerOneSession.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Last().Arguments[0]);
        Assert.Equal(9001u, update.PublicEventId);

        PublicEventTeamStats teamStats = Assert.Single(update.TeamStats);
        Assert.Equal(PublicEventTeamId.PublicTeam, teamStats.TeamId);
        AssertStats(teamStats.Stats, 0x00000009u, 27u, 2u);

        PublicEventParticipantStats one = update.ParticipantStats.Single(s => s.Player.Id == 101ul);
        Assert.Equal(1001u, one.UnitId);
        Assert.Equal(Class.Warrior, one.Class);
        Assert.Equal(PlayerPath.Soldier, one.Path);
        Assert.Equal(77, one.Player.RealmId);
        AssertStats(one.Stats, 0x00000001u, 12u);

        PublicEventParticipantStats two = update.ParticipantStats.Single(s => s.Player.Id == 102ul);
        Assert.Equal(1002u, two.UnitId);
        Assert.Equal(Class.Esper, two.Class);
        Assert.Equal(PlayerPath.Scientist, two.Path);
        AssertStats(two.Stats, 0x00000009u, 15u, 2u);
    }

    [Fact]
    public void SendScoreboardUpdate_IgnoresPlayerOutsideEvent()
    {
        PublicEventHarness harness = CreatePublicEventHarness();

        harness.Event.SendScoreboardUpdate(harness.PlayerOne);

        Assert.Empty(harness.PlayerOneSession.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void IncrementStat_AddsDeltasWithoutChangingAbsoluteUpdateSemantics()
    {
        PublicEventHarness harness = CreatePublicEventHarness();

        harness.Event.JoinEvent(harness.PlayerOne, PublicEventTeamId.PublicTeam);
        harness.Event.JoinEvent(harness.PlayerTwo, PublicEventTeamId.PublicTeam);
        harness.Event.IncrementStat(harness.PlayerOne, PublicEventStat.Damage, 10u);
        harness.Event.IncrementStat(harness.PlayerOne, PublicEventStat.Damage, 5u);
        harness.Event.UpdateStat(harness.PlayerTwo, PublicEventStat.Damage, 20u);
        harness.Event.UpdateStat(harness.PlayerTwo, PublicEventStat.Damage, 7u);

        harness.Event.SendScoreboardUpdate(harness.PlayerOne);

        ServerPublicEventStatsUpdate update = Assert.IsType<ServerPublicEventStatsUpdate>(
            harness.PlayerOneSession.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Last().Arguments[0]);

        PublicEventTeamStats teamStats = Assert.Single(update.TeamStats);
        AssertStats(teamStats.Stats, 0x00000001u, 22u);

        PublicEventParticipantStats one = update.ParticipantStats.Single(s => s.Player.Id == 101ul);
        AssertStats(one.Stats, 0x00000001u, 15u);

        PublicEventParticipantStats two = update.ParticipantStats.Single(s => s.Player.Id == 102ul);
        AssertStats(two.Stats, 0x00000001u, 7u);
    }

    [Fact]
    public void UpdateCustomStat_UsesTableBackedStatIndex()
    {
        PublicEventHarness harness = CreatePublicEventHarness(new PublicEventCustomStatEntry
        {
            Id = 1u,
            PublicEventId = 9001u,
            StatIndex = 2u
        });

        harness.Event.JoinEvent(harness.PlayerOne, PublicEventTeamId.PublicTeam);
        harness.Event.JoinEvent(harness.PlayerTwo, PublicEventTeamId.PublicTeam);
        harness.Event.UpdateCustomStat(harness.PlayerOne, 2u, 5u);
        harness.Event.UpdateCustomStat(harness.PlayerTwo, 2u, 7u);
        harness.Event.UpdateCustomStat(harness.PlayerOne, 0u, 99u);
        harness.Event.UpdateCustomStat(harness.PlayerTwo, 2u, 9u);

        harness.Event.SendScoreboardUpdate(harness.PlayerOne);

        ServerPublicEventStatsUpdate update = Assert.IsType<ServerPublicEventStatsUpdate>(
            harness.PlayerOneSession.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)).Last().Arguments[0]);

        PublicEventTeamStats teamStats = Assert.Single(update.TeamStats);
        AssertStats(teamStats.Stats, 0x00100000u, 14u);

        PublicEventParticipantStats one = update.ParticipantStats.Single(s => s.Player.Id == 101ul);
        AssertStats(one.Stats, 0x00100000u, 5u);

        PublicEventParticipantStats two = update.ParticipantStats.Single(s => s.Player.Id == 102ul);
        AssertStats(two.Stats, 0x00100000u, 9u);
    }

    [Fact]
    public void ScoreboardHandler_SubscribeSendsSnapshotForRequestedEvent()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> managerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        managerProxy.SetMethodReturn(nameof(IPublicEventManager.GetEvent), publicEvent);

        var handler = new ClientPublicEventRequestScoreboardHandler(NullLogger<ClientPublicEventRequestScoreboardHandler>.Instance);
        handler.HandleMessage(session, CreateRequest(publicEventId: 9001u, subscribe: true));

        RecordingDispatchProxy<IPublicEventManager>.Invocation getEvent = Assert.Single(managerProxy.GetInvocations(nameof(IPublicEventManager.GetEvent)));
        Assert.Equal(9001u, getEvent.Arguments[0]);

        RecordingDispatchProxy<IPublicEvent>.Invocation send = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SendScoreboardUpdate)));
        Assert.Same(player, send.Arguments[0]);
    }

    [Fact]
    public void ScoreboardHandler_UnsubscribeDoesNotSendSnapshot()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> managerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);

        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        managerProxy.SetMethodReturn(nameof(IPublicEventManager.GetEvent), publicEvent);

        var handler = new ClientPublicEventRequestScoreboardHandler(NullLogger<ClientPublicEventRequestScoreboardHandler>.Instance);
        handler.HandleMessage(session, CreateRequest(publicEventId: 9001u, subscribe: false));

        Assert.Empty(playerProxy.GetInvocations("get_" + nameof(IPlayer.Map)));
        Assert.Empty(managerProxy.GetInvocations(nameof(IPublicEventManager.GetEvent)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.SendScoreboardUpdate)));
    }

    private static PublicEventHarness CreatePublicEventHarness(params PublicEventCustomStatEntry[] customStats)
    {
        IGameSession playerOneSession = RecordingDispatchProxy<IGameSession>.Create(out RecordingDispatchProxy<IGameSession> playerOneSessionProxy);
        IGameSession playerTwoSession = RecordingDispatchProxy<IGameSession>.Create(out _);

        IPlayer playerOne = TestPlayerBuilder.Create()
            .WithCharacterId(101ul)
            .WithGuid(1001u)
            .WithSession(playerOneSession)
            .Build();
        IPlayer playerTwo = TestPlayerBuilder.Create()
            .WithCharacterId(102ul)
            .WithGuid(1002u)
            .WithSession(playerTwoSession)
            .Build();

        RecordingDispatchProxy<IPlayer> playerOneProxy = (RecordingDispatchProxy<IPlayer>)(object)playerOne;
        playerOneProxy.SetProperty(nameof(IPlayer.Class), Class.Warrior);
        playerOneProxy.SetProperty(nameof(IPlayer.Path), PlayerPath.Soldier);

        RecordingDispatchProxy<IPlayer> playerTwoProxy = (RecordingDispatchProxy<IPlayer>)(object)playerTwo;
        playerTwoProxy.SetProperty(nameof(IPlayer.Class), Class.Esper);
        playerTwoProxy.SetProperty(nameof(IPlayer.Path), PlayerPath.Scientist);

        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodHandler(nameof(IPlayerManager.GetPlayer), args =>
        {
            return (ulong)args[0] switch
            {
                101ul => playerOne,
                102ul => playerTwo,
                _     => null
            };
        });

        IRealmContext realmContext = RecordingDispatchProxy<IRealmContext>.Create(out RecordingDispatchProxy<IRealmContext> realmProxy);
        realmProxy.SetProperty(nameof(IRealmContext.RealmId), (ushort)77);

        var publicEvent = new NexusForever.Game.PublicEvent.PublicEvent(
            NullLogger<NexusForever.Game.PublicEvent.PublicEvent>.Instance,
            RecordingDispatchProxy<IScriptManager>.Create(out _),
            new PublicEventTestSupport.DelegateFactory<IPublicEventTeam>(() => new RuntimePublicEventTeam(
                NullLogger<RuntimePublicEventTeam>.Instance,
                new PublicEventTestSupport.ThrowingFactory<IPublicEventObjective>(),
                new PublicEventTestSupport.DelegateFactory<IPublicEventTeamMember>(() => new PublicEventTeamMember(
                    NullLogger<PublicEventTeamMember>.Instance,
                    playerManager,
                    realmContext)),
                new PublicEventTestSupport.ThrowingFactory<IPublicEventVote>(),
                new GamePublicEventStats())),
            RecordingDispatchProxy<IPublicEventEntityFactory>.Create(out _));

        IPublicEventManager manager = RecordingDispatchProxy<IPublicEventManager>.Create(out _);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out _);
        publicEvent.Initialise(manager, new ScoreboardTemplate(customStats), map);

        return new PublicEventHarness(publicEvent, playerOne, playerTwo, playerOneSessionProxy);
    }

    private static void AssertStats(NetworkPublicEventStats stats, uint expectedMask, params uint[] expectedValues)
    {
        Assert.Equal(expectedMask, BitConverter.ToUInt32(stats.Mask.GetBuffer()));
        Assert.Equal(expectedValues, stats.Values);
    }

    private static ClientPublicEventRequestScoreboard CreateRequest(uint publicEventId, bool subscribe)
    {
        byte[] packetData;
        using (var stream = new MemoryStream())
        {
            using (var writer = new GamePacketWriter(stream))
            {
                writer.Write(publicEventId, 14u);
                writer.Write(subscribe);
                writer.FlushBits();
            }

            packetData = stream.ToArray();
        }

        using var readStream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(readStream);
        var message = new ClientPublicEventRequestScoreboard();
        message.Read(reader);
        return message;
    }

    private sealed class ScoreboardTemplate(params PublicEventCustomStatEntry[] customStats) : IPublicEventTemplate
    {
        public PublicEventEntry Entry { get; } = new()
        {
            Id = 9001u
        };

        public Dictionary<uint, PublicEventObjectiveEntry> Objectives { get; } = [];
        public List<PublicEventTeamEntry> Teams { get; } =
        [
            new PublicEventTeamEntry
            {
                Id = PublicEventTeamId.PublicTeam
            }
        ];
        public List<PublicEventCustomStatEntry> CustomStats { get; } = [..customStats];
        public IReadOnlyList<uint> Locations { get; } = [];
        public IReadOnlyList<uint> ChildEventIds { get; } = [];

        public void Initialise(PublicEventEntry entry) => throw new NotSupportedException();
        public IReadOnlyList<PublicEventObjectiveStatus.VirtualItem> GetObjectiveVirtualItems(PublicEventObjectiveEntry entry) => [];
        public bool HasLiveStats() => false;
    }

    private sealed record PublicEventHarness(
        NexusForever.Game.PublicEvent.PublicEvent Event,
        IPlayer PlayerOne,
        IPlayer PlayerTwo,
        RecordingDispatchProxy<IGameSession> PlayerOneSession);
}
