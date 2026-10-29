using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Static.Map;
using NexusForever.Game.Static.Map.Lock;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.World.Message.Model;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupMemberRemovedHandler : IHandleMessages<GroupMemberRemovedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;
        private readonly IGroupStateManager groupStateManager;
        private readonly IMapLockManager mapLockManager;

        public GroupMemberRemovedHandler(
            IPlayerManager playerManager,
            IGroupStateManager groupStateManager,
            IMapLockManager mapLockManager = null)
        {
            this.playerManager      = playerManager;
            this.groupStateManager = groupStateManager;
            this.mapLockManager = mapLockManager;
        }

        #endregion

        public Task Handle(GroupMemberRemovedMessage message)
        {
            groupStateManager.UpdateGroup(message.Group.ToGroupLootState());
            groupStateManager.RemoveMember(message.Group.Id, message.RemovedMember.Identity.ToGameIdentity());

            var groupRemove = new ServerGroupRemove
            {
                GroupId      = message.Group.Id,
                Reason       = message.Reason,
                TargetPlayer = message.RemovedMember.Identity.ToNetworkIdentity(),
            };

            playerManager.EnqueueToOnlineMembers(message.Group.Members, groupRemove);

            IPlayer removed = playerManager.GetPlayer(message.RemovedMember.Identity.ToGameIdentity());
            if (removed?.Map is IContentMapInstance instance
                && instance.MapLock?.Type is MapLockType.Group or MapLockType.Raid
                && mapLockManager?.GetGroupLock(message.Group.Id, instance.Entry.Id)?.InstanceId == instance.MapLock.InstanceId)
                instance.RequestPendingRemoval(removed, WorldRemovalReason.GroupMembership);

            return Task.CompletedTask;
        }
    }
}
