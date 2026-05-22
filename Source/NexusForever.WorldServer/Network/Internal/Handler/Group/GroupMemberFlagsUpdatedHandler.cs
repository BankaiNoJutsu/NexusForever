using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
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
            }

            return Task.CompletedTask;
        }
    }
}
