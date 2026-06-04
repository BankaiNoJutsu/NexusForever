using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
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
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Tests.Group;

public class GroupFlagHandlerTests
{
    [Fact]
    public void GroupFlagsChanged_PublishesSourceIdentityAndFlags()
    {
        IInternalMessagePublisher publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out RecordingDispatchProxy<IInternalMessagePublisher> publisherProxy);
        publisherProxy.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        IWorldSession session = CreateWorldSession(101ul);
        var handler = new ClientGroupFlagsChangedHandler(publisher);

        handler.HandleMessage(session, new ClientGroupFlagsChanged
        {
            GroupId  = 9001ul,
            NewFlags = GroupFlags.Raid | GroupFlags.JoinRequestClosed
        });

        RecordingDispatchProxy<IInternalMessagePublisher>.Invocation invocation =
            Assert.Single(publisherProxy.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync)));
        GroupFlagsUpdateMessage message = Assert.IsType<GroupFlagsUpdateMessage>(invocation.Arguments[0]);
        Assert.Equal(9001ul, message.GroupId);
        Assert.Equal(new InternalIdentity { RealmId = 1, Id = 101ul }, message.Identity);
        Assert.Equal(GroupFlags.Raid | GroupFlags.JoinRequestClosed, message.Flags);
    }

    [Fact]
    public void GroupSetRole_PublishesSourceTargetAndChangedFlag()
    {
        IInternalMessagePublisher publisher = RecordingDispatchProxy<IInternalMessagePublisher>.Create(out RecordingDispatchProxy<IInternalMessagePublisher> publisherProxy);
        publisherProxy.SetMethodReturn(nameof(IInternalMessagePublisher.PublishAsync), Task.CompletedTask);
        IWorldSession session = CreateWorldSession(101ul);
        var handler = new ClientGroupSetRoleHandler(publisher);

        handler.HandleMessage(session, new ClientGroupSetRole
        {
            GroupId        = 9001ul,
            TargetedPlayer = new NetworkIdentity { RealmId = 1, Id = 202ul },
            CurrentFlags   = GroupMemberInfoFlags.DPS,
            ChangedFlag    = GroupMemberInfoFlags.Healer
        });

        RecordingDispatchProxy<IInternalMessagePublisher>.Invocation invocation =
            Assert.Single(publisherProxy.GetInvocations(nameof(IInternalMessagePublisher.PublishAsync)));
        GroupMemberFlagUpdateMessage message = Assert.IsType<GroupMemberFlagUpdateMessage>(invocation.Arguments[0]);
        Assert.Equal(9001ul, message.GroupId);
        Assert.Equal(new InternalIdentity { RealmId = 1, Id = 101ul }, message.Source);
        Assert.Equal(new InternalIdentity { RealmId = 1, Id = 202ul }, message.Target);
        Assert.Equal(GroupMemberInfoFlags.Healer, message.Flags);
    }

    [Fact]
    public async Task GroupFlagsUpdated_BroadcastsFlagsToOnlineMembers()
    {
        var playerManager = new TestPlayerManager();
        IPlayer alice = CreatePlayer(101ul, out RecordingDispatchProxy<IGameSession> aliceSessionProxy);
        IPlayer bob = CreatePlayer(202ul, out RecordingDispatchProxy<IGameSession> bobSessionProxy);
        playerManager.AddPlayer(alice);
        playerManager.AddPlayer(bob);
        var handler = new GroupFlagsUpdatedHandler(playerManager);

        await handler.Handle(new GroupFlagsUpdatedMessage
        {
            Group = new InternalGroup
            {
                Id      = 9001ul,
                Flags   = GroupFlags.Raid | GroupFlags.ReferralsOpen,
                Members =
                [
                    CreateGroupMember(101ul),
                    CreateGroupMember(202ul),
                    CreateGroupMember(303ul)
                ]
            }
        });

        ServerGroupFlagsChanged aliceMessage = AssertEncryptedMessage<ServerGroupFlagsChanged>(aliceSessionProxy);
        ServerGroupFlagsChanged bobMessage = AssertEncryptedMessage<ServerGroupFlagsChanged>(bobSessionProxy);
        Assert.Equal(9001ul, aliceMessage.GroupId);
        Assert.Equal(GroupFlags.Raid | GroupFlags.ReferralsOpen, aliceMessage.Flags);
        Assert.Equal(aliceMessage.GroupId, bobMessage.GroupId);
        Assert.Equal(aliceMessage.Flags, bobMessage.Flags);
    }

    [Fact]
    public async Task GroupMemberFlagsUpdated_BroadcastsTargetMemberFlagsToOnlineMembers()
    {
        var playerManager = new TestPlayerManager();
        IPlayer alice = CreatePlayer(101ul, out RecordingDispatchProxy<IGameSession> aliceSessionProxy);
        IPlayer bob = CreatePlayer(202ul, out RecordingDispatchProxy<IGameSession> bobSessionProxy);
        playerManager.AddPlayer(alice);
        playerManager.AddPlayer(bob);
        InternalGroupMember targetMember = CreateGroupMember(202ul, GroupMemberInfoFlags.Healer | GroupMemberInfoFlags.CanMark);
        var handler = new GroupMemberFlagsUpdatedHandler(playerManager);

        await handler.Handle(new GroupMemberFlagsUpdatedMessage
        {
            Group = new InternalGroup
            {
                Id      = 9001ul,
                Members =
                [
                    CreateGroupMember(101ul),
                    targetMember,
                    CreateGroupMember(303ul)
                ]
            },
            Member        = targetMember,
            FromPromotion = true
        });

        ServerGroupMemberFlagsChanged aliceMessage = AssertEncryptedMessage<ServerGroupMemberFlagsChanged>(aliceSessionProxy);
        ServerGroupMemberFlagsChanged bobMessage = AssertEncryptedMessage<ServerGroupMemberFlagsChanged>(bobSessionProxy);
        Assert.Equal(9001ul, aliceMessage.GroupId);
        Assert.Equal(1, aliceMessage.TargetedPlayer.RealmId);
        Assert.Equal(202ul, aliceMessage.TargetedPlayer.Id);
        Assert.Equal(GroupMemberInfoFlags.Healer | GroupMemberInfoFlags.CanMark, aliceMessage.ChangedFlags);
        Assert.True(aliceMessage.IsFromPromotion);
        Assert.Equal(aliceMessage.GroupId, bobMessage.GroupId);
        Assert.Equal(aliceMessage.TargetedPlayer.Id, bobMessage.TargetedPlayer.Id);
        Assert.Equal(aliceMessage.ChangedFlags, bobMessage.ChangedFlags);
        Assert.Equal(aliceMessage.IsFromPromotion, bobMessage.IsFromPromotion);
    }

    [Fact]
    public async Task GroupMemberFlagsUpdated_BroadcastsRoleChangeWhenNotPromotion()
    {
        var playerManager = new TestPlayerManager();
        IPlayer alice = CreatePlayer(101ul, out RecordingDispatchProxy<IGameSession> aliceSessionProxy);
        IPlayer bob = CreatePlayer(202ul, out RecordingDispatchProxy<IGameSession> bobSessionProxy);
        playerManager.AddPlayer(alice);
        playerManager.AddPlayer(bob);
        InternalGroupMember targetMember = CreateGroupMember(202ul, GroupMemberInfoFlags.Healer);
        var handler = new GroupMemberFlagsUpdatedHandler(playerManager);

        await handler.Handle(new GroupMemberFlagsUpdatedMessage
        {
            Group = new InternalGroup
            {
                Id      = 9001ul,
                Members = [CreateGroupMember(101ul), targetMember]
            },
            Member        = targetMember,
            FromPromotion = false
        });

        ServerGroupIdentityListAndUInt32Array aliceMessage = AssertEncryptedMessage<ServerGroupIdentityListAndUInt32Array>(aliceSessionProxy);
        ServerGroupIdentityListAndUInt32Array bobMessage   = AssertEncryptedMessage<ServerGroupIdentityListAndUInt32Array>(bobSessionProxy);
        Assert.Equal(9001ul, aliceMessage.GroupId);
        Assert.Equal([(uint)GroupMemberInfoFlags.Healer], aliceMessage.Values);
        Assert.Equal(aliceMessage.GroupId, bobMessage.GroupId);
        Assert.Equal(aliceMessage.Values, bobMessage.Values);
    }

    [Fact]
    public async Task GroupMemberFlagsUpdated_BroadcastsReadyCheckStatusWhenPending()
    {
        var playerManager = new TestPlayerManager();
        IPlayer alice = CreatePlayer(101ul, out RecordingDispatchProxy<IGameSession> aliceSessionProxy);
        IPlayer bob = CreatePlayer(202ul, out RecordingDispatchProxy<IGameSession> bobSessionProxy);
        playerManager.AddPlayer(alice);
        playerManager.AddPlayer(bob);
        InternalGroupMember targetMember = CreateGroupMember(202ul, GroupMemberInfoFlags.Pending | GroupMemberInfoFlags.Healer);
        var handler = new GroupMemberFlagsUpdatedHandler(playerManager);

        await handler.Handle(new GroupMemberFlagsUpdatedMessage
        {
            Group = new InternalGroup
            {
                Id      = 9001ul,
                Members = [CreateGroupMember(101ul), targetMember]
            },
            Member        = targetMember,
            FromPromotion = false
        });

        ServerGroupIdentityListAndUInt32Array roleChange = AssertEncryptedMessage<ServerGroupIdentityListAndUInt32Array>(aliceSessionProxy);
        ServerGroupReadyCheckStatusUpdate readyCheck = AssertEncryptedMessage<ServerGroupReadyCheckStatusUpdate>(bobSessionProxy, 1);

        Assert.Equal(9001ul, roleChange.GroupId);
        Assert.Equal(9001ul, readyCheck.GroupId);
        Assert.Equal(202ul, readyCheck.MemberIdentity.Id);
        Assert.Equal(0u, readyCheck.ReadyStatus);
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

    private static InternalGroupMember CreateGroupMember(ulong characterId, GroupMemberInfoFlags flags = GroupMemberInfoFlags.None)
    {
        return new InternalGroupMember
        {
            Identity = new InternalIdentity
            {
                RealmId = 1,
                Id      = characterId
            },
            Flags = flags
        };
    }

    private static T AssertEncryptedMessage<T>(RecordingDispatchProxy<IGameSession> sessionProxy, int index = 0)
    {
        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> invocations =
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        RecordingDispatchProxy<IGameSession>.Invocation invocation = invocations[index];
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
