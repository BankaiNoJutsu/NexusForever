using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Group;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.Message;
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
            uint memberIndex = message.Member.GroupIndex;
            IWritable flagsMessage;
            if (message.FromPromotion)
            {
                flagsMessage = new ServerGroupMemberFlagsChanged
                {
                    GroupId         = message.Group.Id,
                    MemberIndex     = memberIndex,
                    TargetedPlayer  = message.Member.Identity.ToNetworkIdentity(),
                    ChangedFlags    = message.Member.Flags,
                    IsFromPromotion = true,
                };
            }
            else
            {
                // Provisional runtime emitter: the native shape is mapped, but the leading
                // value and parallel uint32 array semantics still need consumer proof.
                var roleChange = new ServerGroupIdentityListAndUInt32Array
                {
                    GroupId      = message.Group.Id,
                    LeadingValue = memberIndex,
                };
                roleChange.MemberIdentities.Add(message.Member.Identity.ToNetworkIdentity());
                roleChange.Values.Add((uint)message.Member.Flags);
                flagsMessage = roleChange;
            }

            var readyCheckMessage = new ServerGroupReadyCheckStatusUpdate
            {
                GroupId        = message.Group.Id,
                MemberIdentity = message.Member.Identity.ToNetworkIdentity(),
                ReadyStatus    = BuildReadyCheckStatus(message.Member.Flags),
            };

            foreach (IPlayer player in playerManager.GetOnlineGroupMembers(message.Group.Members))
            {
                player.Session.EnqueueMessageEncrypted(flagsMessage);
                player.Session.EnqueueMessageEncrypted(readyCheckMessage);
            }

            return Task.CompletedTask;
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
