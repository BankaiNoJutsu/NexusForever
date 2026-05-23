using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Group;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.Internal.Message.Group.Shared;
using NexusForever.Network.World.Message.Model;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupMemberFlagsUpdatedHandler : IHandleMessages<GroupMemberFlagsUpdatedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;

        public GroupMemberFlagsUpdatedHandler(
            IPlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        #endregion

        public Task Handle(GroupMemberFlagsUpdatedMessage message)
        {
            foreach (GroupMember item in message.Group.Members)
            {
                IPlayer player = playerManager.GetPlayer(item.Identity.ToGameIdentity());
                if (player == null)
                    continue;

                if (message.FromPromotion)
                {
                    player.Session.EnqueueMessageEncrypted(new ServerGroupMemberFlagsChanged
                    {
                        GroupId         = message.Group.Id,
                        TargetedPlayer  = message.Member.Identity.ToNetworkIdentity(),
                        ChangedFlags    = message.Member.Flags,
                        IsFromPromotion = true,
                    });
                }
                else
                {
                    player.Session.EnqueueMessageEncrypted(new ServerGroupMemberRoleChange
                    {
                        GroupId        = message.Group.Id,
                        TargetedPlayer = message.Member.Identity.ToNetworkIdentity(),
                        ChangedFlags   = message.Member.Flags,
                    });
                }

                if (HasReadyCheckFlags(message.Member.Flags))
                {
                    player.Session.EnqueueMessageEncrypted(new ServerGroupReadyCheckStatusUpdate
                    {
                        GroupId        = message.Group.Id,
                        MemberIdentity = message.Member.Identity.ToNetworkIdentity(),
                        ReadyStatus    = BuildReadyCheckStatus(message.Member.Flags),
                    });
                }
            }

            return Task.CompletedTask;
        }

        private static bool HasReadyCheckFlags(GroupMemberInfoFlags flags)
        {
            const GroupMemberInfoFlags readyCheckMask = GroupMemberInfoFlags.Pending
                | GroupMemberInfoFlags.Ready
                | GroupMemberInfoFlags.HasSetReady;

            return (flags & readyCheckMask) != 0;
        }

        private static uint BuildReadyCheckStatus(GroupMemberInfoFlags flags)
        {
            if (flags.HasFlag(GroupMemberInfoFlags.Ready))
                return 2u;

            if (flags.HasFlag(GroupMemberInfoFlags.HasSetReady))
                return 1u;

            if (flags.HasFlag(GroupMemberInfoFlags.Pending))
                return 0u;

            return 0u;
        }
    }
}
