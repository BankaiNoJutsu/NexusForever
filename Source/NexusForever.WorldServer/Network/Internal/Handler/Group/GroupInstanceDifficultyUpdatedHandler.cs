using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Network.World.Message.Model;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Group
{
    public class GroupInstanceDifficultyUpdatedHandler : IHandleMessages<GroupInstanceDifficultyUpdatedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;
        private readonly IGroupStateManager groupStateManager;

        public GroupInstanceDifficultyUpdatedHandler(
            IPlayerManager playerManager,
            IGroupStateManager groupStateManager)
        {
            this.playerManager      = playerManager;
            this.groupStateManager = groupStateManager;
        }

        #endregion

        public Task Handle(GroupInstanceDifficultyUpdatedMessage message)
        {
            GroupLootState group = message.Group.ToGroupLootState();
            groupStateManager.UpdateGroup(group);

            IPlayer setter = message.Setter != null
                ? playerManager.GetPlayer(message.Setter.ToGameIdentity())
                : null;

            var groupInstanceDifficultyResponse = new ServerGroupInstanceDifficultyResponse
            {
                GroupId       = message.Group.Id,
                CharacterGuid = setter?.Guid ?? 0u,
                Difficulty    = message.Group.InstanceDifficulty,
                Unknown0      = 0u
            };

            foreach (IPlayer player in playerManager.GetOnlineGroupMembers(message.Group.Members))
            {
                player.InstanceDifficulty = message.Group.InstanceDifficulty;
                player.Session.EnqueueMessageEncrypted(groupInstanceDifficultyResponse);
            }

            return Task.CompletedTask;
        }
    }
}
