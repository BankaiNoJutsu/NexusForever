using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Group;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Matching;
using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Tests.Matching;

public class ClientMatchingQueueLeaveAsGroupHandlerTests
{
    [Fact]
    public void HandleMessage_WithGroupLeavesQueueForOnlineMembers()
    {
        Identity leaderIdentity = new()
        {
            RealmId = 1,
            Id      = 10ul
        };
        Identity memberIdentity = new()
        {
            RealmId = 1,
            Id      = 20ul
        };

        IWorldSession session = CreateSession(leaderIdentity, out IPlayer leader);
        IPlayer onlineMember = CreatePlayer(memberIdentity);
        var groupStateManager = new GroupStateManager();
        groupStateManager.UpdateGroup(new GroupLootState
        {
            GroupId          = 77ul,
            NormalRule       = LootRule.NeedBeforeGreed,
            ThresholdRule    = LootRule.Master,
            ThresholdQuality = LootThreshold.Superb,
            HarvestRule      = HarvestLootRule.RoundRobin,
            Leader           = leaderIdentity,
            Members =
            [
                new GroupLootMember
                {
                    Identity   = leaderIdentity,
                    GroupIndex = 0u
                },
                new GroupLootMember
                {
                    Identity   = memberIdentity,
                    GroupIndex = 1u
                }
            ]
        });

        ClientMatchingQueueLeaveAsGroupHandler handler = CreateHandler(
            groupStateManager,
            onlineMember,
            out RecordingDispatchProxy<IMatchingManager> matchingManagerProxy);

        handler.HandleMessage(session, CreateRequest(MatchType.Dungeon));

        IReadOnlyList<RecordingDispatchProxy<IMatchingManager>.Invocation> leaveCalls =
            matchingManagerProxy.GetInvocations(nameof(IMatchingManager.LeaveQueue));
        Assert.Collection(leaveCalls,
            call =>
            {
                Assert.Same(leader, call.Arguments[0]);
                Assert.Equal(MatchType.Dungeon, call.Arguments[1]);
            },
            call =>
            {
                Assert.Same(onlineMember, call.Arguments[0]);
                Assert.Equal(MatchType.Dungeon, call.Arguments[1]);
            });
    }

    [Fact]
    public void HandleMessage_WithoutGroupLeavesQueueForRequesterOnly()
    {
        Identity identity = new()
        {
            RealmId = 1,
            Id      = 10ul
        };

        IWorldSession session = CreateSession(identity, out IPlayer player);
        ClientMatchingQueueLeaveAsGroupHandler handler = CreateHandler(
            new GroupStateManager(),
            onlineMember: null,
            out RecordingDispatchProxy<IMatchingManager> matchingManagerProxy);

        handler.HandleMessage(session, CreateRequest(MatchType.Adventure));

        RecordingDispatchProxy<IMatchingManager>.Invocation leaveCall =
            Assert.Single(matchingManagerProxy.GetInvocations(nameof(IMatchingManager.LeaveQueue)));
        Assert.Same(player, leaveCall.Arguments[0]);
        Assert.Equal(MatchType.Adventure, leaveCall.Arguments[1]);
    }

    private static ClientMatchingQueueLeaveAsGroupHandler CreateHandler(
        IGroupStateManager groupStateManager,
        IPlayer onlineMember,
        out RecordingDispatchProxy<IMatchingManager> matchingManagerProxy)
    {
        IMatchingManager matchingManager = RecordingDispatchProxy<IMatchingManager>.Create(out matchingManagerProxy);
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out RecordingDispatchProxy<IPlayerManager> playerManagerProxy);
        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), onlineMember);

        return new ClientMatchingQueueLeaveAsGroupHandler(
            NullLogger<ClientMatchingQueueLeaveAsGroupHandler>.Instance,
            matchingManager,
            groupStateManager,
            playerManager);
    }

    private static IWorldSession CreateSession(Identity identity, out IPlayer player)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        player = CreatePlayer(identity);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IPlayer CreatePlayer(Identity identity)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Identity), identity);
        playerProxy.SetProperty(nameof(IPlayer.Guid), (uint)identity.Id);
        return player;
    }

    private static ClientMatchingQueueLeaveAsGroup CreateRequest(MatchType matchType)
    {
        byte[] packetData = WritePacket(writer => writer.Write(matchType, 5u));
        using var reader = new GamePacketReader(new MemoryStream(packetData));

        var request = new ClientMatchingQueueLeaveAsGroup();
        request.Read(reader);
        return request;
    }

    private static byte[] WritePacket(Action<GamePacketWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }
}
