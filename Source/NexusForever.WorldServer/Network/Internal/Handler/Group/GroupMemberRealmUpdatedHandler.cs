using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.World.Message.Model;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupMemberRealmUpdatedHandler : IHandleMessages<GroupMemberRealmUpdatedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;

        public GroupMemberRealmUpdatedHandler(
            IPlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        #endregion

        public Task Handle(GroupMemberRealmUpdatedMessage message)
        {
            var groupUpdatePlayerRealm = new ServerGroupUpdatePlayerRealm
            {
                GroupId              = message.Group.Id,
                TargetPlayerIdentity = message.Member.Identity.ToNetworkIdentity(),
                RealmId              = message.Member.Character.RealmId,
                ZoneId               = message.Member.Character.WorldZoneId,
                MapId                = message.Member.Character.WorldId,
                PhaseId              = 1,
                IsSyncdToGroup       = true
            };

            playerManager.EnqueueToOnlineMembers(message.Group.Members, groupUpdatePlayerRealm);

            return Task.CompletedTask;
        }
    }
}
