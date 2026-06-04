using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Static.Group;
using NexusForever.Game.Static.Setting;
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

public class GroupInstanceDifficultyHandlerTests
{
    [Fact]
    public void ClientGroupSetInstanceDifficulty_PublishesAuthoritativeUpdateRequest()
    {
        IInternalMessagePublisher publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out RecordingDispatchProxy<IInternalMessagePublisher> publisherProxy);
        publisherProxy.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        IWorldSession session = CreateWorldSession(101ul, playerGuid: 5001u, WorldDifficulty.Normal);
        var handler = new ClientGroupSetInstanceDifficultyHandler(
            NullLogger<ClientGroupSetInstanceDifficultyHandler>.Instance,
            publisher);

        handler.HandleMessage(session, CreateSetDifficulty(9001ul, WorldDifficulty.Veteran));

        RecordingDispatchProxy<IInternalMessagePublisher>.Invocation invocation =
            Assert.Single(publisherProxy.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync)));
        GroupInstanceDifficultyUpdateMessage message = Assert.IsType<GroupInstanceDifficultyUpdateMessage>(invocation.Arguments[0]);
        Assert.Equal(9001ul, message.GroupId);
        Assert.Equal(new InternalIdentity { RealmId = 1, Id = 101ul }, message.Identity);
        Assert.Equal(WorldDifficulty.Veteran, message.Difficulty);
        Assert.Equal(WorldDifficulty.Normal, session.Player.InstanceDifficulty);
    }

    [Fact]
    public async Task GroupInstanceDifficultyUpdated_UpdatesStateMembersAndBroadcasts()
    {
        var playerManager = new TestPlayerManager();
        IPlayer alice = CreatePlayer(101ul, playerGuid: 5001u, WorldDifficulty.Normal, out RecordingDispatchProxy<IGameSession> aliceSessionProxy);
        IPlayer bob = CreatePlayer(202ul, playerGuid: 5002u, WorldDifficulty.Normal, out RecordingDispatchProxy<IGameSession> bobSessionProxy);
        playerManager.AddPlayer(alice);
        playerManager.AddPlayer(bob);
        IGroupStateManager groupStateManager = RecordingDispatchProxy<IGroupStateManager>.Create(out RecordingDispatchProxy<IGroupStateManager> groupStateProxy);
        var handler = new GroupInstanceDifficultyUpdatedHandler(playerManager, groupStateManager);

        await handler.Handle(new GroupInstanceDifficultyUpdatedMessage
        {
            Setter = new InternalIdentity { RealmId = 1, Id = 101ul },
            Group = new InternalGroup
            {
                Id                 = 9001ul,
                InstanceDifficulty = WorldDifficulty.Veteran,
                Leader             = new InternalIdentity { RealmId = 1, Id = 101ul },
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
        Assert.Equal(WorldDifficulty.Veteran, state.InstanceDifficulty);

        Assert.Equal(WorldDifficulty.Veteran, alice.InstanceDifficulty);
        Assert.Equal(WorldDifficulty.Veteran, bob.InstanceDifficulty);

        ServerGroupInstanceDifficultyResponse aliceMessage = AssertEncryptedMessage<ServerGroupInstanceDifficultyResponse>(aliceSessionProxy);
        ServerGroupInstanceDifficultyResponse bobMessage = AssertEncryptedMessage<ServerGroupInstanceDifficultyResponse>(bobSessionProxy);
        Assert.Equal(9001ul, aliceMessage.GroupId);
        Assert.Equal(5001u, aliceMessage.CharacterGuid);
        Assert.Equal(WorldDifficulty.Veteran, aliceMessage.Difficulty);
        Assert.Equal(aliceMessage.GroupId, bobMessage.GroupId);
        Assert.Equal(aliceMessage.CharacterGuid, bobMessage.CharacterGuid);
        Assert.Equal(aliceMessage.Difficulty, bobMessage.Difficulty);
    }

    private static IWorldSession CreateWorldSession(ulong characterId, uint playerGuid, WorldDifficulty difficulty)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), CreatePlayer(characterId, playerGuid, difficulty, out _));
        return session;
    }

    private static IPlayer CreatePlayer(
        ulong characterId,
        uint playerGuid,
        WorldDifficulty difficulty,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new GameIdentity
        {
            RealmId = 1,
            Id      = characterId
        });
        playerProxy.SetProperty(nameof(IPlayer.Guid), playerGuid);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.InstanceDifficulty), difficulty);
        return player;
    }

    private static ClientGroupSetInstanceDifficulty CreateSetDifficulty(ulong groupId, WorldDifficulty difficulty)
    {
        var message = new ClientGroupSetInstanceDifficulty();
        SetProperty(message, nameof(ClientGroupSetInstanceDifficulty.GroupId), groupId);
        SetProperty(message, nameof(ClientGroupSetInstanceDifficulty.Difficulty), difficulty);
        return message;
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

    private static void SetProperty(object instance, string propertyName, object value)
    {
        PropertyInfo property = instance.GetType().GetProperty(propertyName)!;
        property.GetSetMethod(true)!.Invoke(instance, [value]);
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
