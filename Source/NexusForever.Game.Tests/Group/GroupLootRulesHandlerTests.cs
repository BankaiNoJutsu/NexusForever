using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Internal.Handler.Group;
using NexusForever.WorldServer.Network.Message.Handler.Group;
using GameIdentity = NexusForever.Game.Abstract.Identity;
using InternalGroup = NexusForever.Network.Internal.Message.Group.Shared.Group;
using InternalGroupMember = NexusForever.Network.Internal.Message.Group.Shared.GroupMember;
using InternalIdentity = NexusForever.Network.Internal.Message.Shared.Identity;

namespace NexusForever.Game.Tests.Group;

public class GroupLootRulesHandlerTests
{
    [Fact]
    public void ClientGroupLootRulesChange_PublishesMappedRules()
    {
        IInternalMessagePublisher publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out RecordingDispatchProxy<IInternalMessagePublisher> publisherProxy);
        publisherProxy.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        IWorldSession session = CreateWorldSession(101ul);
        var handler = new ClientGroupLootRulesChangeHandler(publisher);

        handler.HandleMessage(session, new ClientGroupLootRulesChange
        {
            GroupId                   = 9001ul,
            LootRulesUnderThreshold   = LootRule.NeedBeforeGreed,
            LootRulesThresholdAndOver = LootRule.Master,
            Threshold                 = LootThreshold.Superb,
            HarvestingRule            = HarvestLootRule.FirstTagger
        });

        RecordingDispatchProxy<IInternalMessagePublisher>.Invocation invocation =
            Assert.Single(publisherProxy.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync)));
        GroupLootRulesUpdateMessage message = Assert.IsType<GroupLootRulesUpdateMessage>(invocation.Arguments[0]);
        Assert.Equal(9001ul, message.GroupId);
        Assert.Equal(new InternalIdentity { RealmId = 1, Id = 101ul }, message.Identity);
        Assert.Equal(LootRule.NeedBeforeGreed, message.NormalRule);
        Assert.Equal(LootRule.Master, message.ThresholdRule);
        Assert.Equal(LootThreshold.Superb, message.ThresholdQuality);
        Assert.Equal(HarvestLootRule.FirstTagger, message.HarvestRule);
    }

    [Fact]
    public async Task GroupLootRulesUpdated_UpdatesStateAndBroadcastsRules()
    {
        var playerManager = new TestPlayerManager();
        IPlayer alice = CreatePlayer(101ul, out RecordingDispatchProxy<IGameSession> aliceSessionProxy);
        IPlayer bob = CreatePlayer(202ul, out RecordingDispatchProxy<IGameSession> bobSessionProxy);
        playerManager.AddPlayer(alice);
        playerManager.AddPlayer(bob);
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out RecordingDispatchProxy<IGroupStateManager> groupStateProxy);
        var handler = new GroupLootRulesUpdatedHandler(playerManager, groupStateManager);

        await handler.Handle(new GroupLootRulesUpdatedMessage
        {
            Group = new InternalGroup
            {
                Id               = 9001ul,
                Leader           = new InternalIdentity { RealmId = 1, Id = 101ul },
                NormalRule       = LootRule.RoundRobin,
                ThresholdRule    = LootRule.Master,
                ThresholdQuality = LootThreshold.Good,
                HarvestRule      = HarvestLootRule.RoundRobin,
                Members =
                [
                    CreateGroupMember(101ul, 0u),
                    CreateGroupMember(202ul, 1u)
                ]
            }
        });

        RecordingDispatchProxy<IGroupStateManager>.Invocation updateCall =
            Assert.Single(groupStateProxy.GetInvocations(nameof(IGroupStateManager.UpdateGroup)));
        GroupLootState state = Assert.IsType<GroupLootState>(updateCall.Arguments[0]);
        Assert.Equal(9001ul, state.GroupId);
        Assert.Equal(LootRule.RoundRobin, state.NormalRule);
        Assert.Equal(LootRule.Master, state.ThresholdRule);
        Assert.Equal(LootThreshold.Good, state.ThresholdQuality);
        Assert.Equal(HarvestLootRule.RoundRobin, state.HarvestRule);

        ServerGroupLootRulesChange aliceMessage = AssertEncryptedMessage<ServerGroupLootRulesChange>(aliceSessionProxy);
        ServerGroupLootRulesChange bobMessage = AssertEncryptedMessage<ServerGroupLootRulesChange>(bobSessionProxy);
        Assert.Equal(9001ul, aliceMessage.GroupId);
        Assert.Equal(LootRule.RoundRobin, aliceMessage.LootRulesUnderThreshold);
        Assert.Equal(LootRule.Master, aliceMessage.LootRulesThresholdAndOver);
        Assert.Equal(LootThreshold.Good, aliceMessage.LootThreshold);
        Assert.Equal(HarvestLootRule.RoundRobin, aliceMessage.HarvestLootRule);
        Assert.Equal(aliceMessage.GroupId, bobMessage.GroupId);
        Assert.Equal(aliceMessage.LootRulesUnderThreshold, bobMessage.LootRulesUnderThreshold);
    }

    private static IWorldSession CreateWorldSession(ulong characterId)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), CreatePlayer(characterId, out _));
        return session;
    }

    private static IPlayer CreatePlayer(ulong characterId, out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new GameIdentity
        {
            RealmId = 1,
            Id      = characterId
        });
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static InternalGroupMember CreateGroupMember(ulong characterId, uint groupIndex)
    {
        return new InternalGroupMember
        {
            Identity = new InternalIdentity
            {
                RealmId = 1,
                Id      = characterId
            },
            GroupIndex = groupIndex
        };
    }

    private static T AssertEncryptedMessage<T>(RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        RecordingDispatchProxy<IGameSession>.Invocation invocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        return Assert.IsType<T>(invocation.Arguments[0]);
    }

    private sealed class TestPlayerManager : IPlayerManager
    {
        private readonly Dictionary<GameIdentity, IPlayer> players = [];

        public void AddPlayer(IPlayer player)
        {
            players[player.Identity] = player;
        }

        public void RemovePlayer(IPlayer player)
        {
            players.Remove(player.Identity);
        }

        public IPlayer GetPlayer(ulong characterId)
        {
            return players.Values.FirstOrDefault(player => player.Identity.Id == characterId);
        }

        public IPlayer GetPlayer(string name)
        {
            return null;
        }

        public IPlayer GetPlayer(GameIdentity identity)
        {
            return players.GetValueOrDefault(identity);
        }

        public IPlayer GetPlayerByAccountId(uint accountId)
        {
            return null;
        }

        public IEnumerator<IPlayer> GetEnumerator()
        {
            return players.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
