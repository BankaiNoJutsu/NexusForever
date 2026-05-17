using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Group;
using NexusForever.Network.Internal.Message.Player;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network.Internal.Handler.Group;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Player
{
    public class PlayerGroupAssociationUpdatedHandler : IHandleMessages<PlayerGroupAssociationUpdatedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;
        private readonly IGroupStateManager groupStateManager;

        public PlayerGroupAssociationUpdatedHandler(
            IPlayerManager playerManager,
            IGroupStateManager groupStateManager)
        {
            this.playerManager      = playerManager;
            this.groupStateManager = groupStateManager;
        }

        #endregion

        public Task Handle(PlayerGroupAssociationUpdatedMessage message)
        {
            if (message.Group != null)
                groupStateManager.UpdateGroup(message.Group.ToGroupLootState());
            else
                groupStateManager.RemoveCharacter(message.Identity.ToGameIdentity());

            IPlayer player = playerManager.GetPlayer(message.Identity.ToGameIdentity());
            if (player == null)
                return Task.CompletedTask;

            return player.SynchroniseAsync(() =>
            {
                player.GroupAssociation = message.Group?.Id ?? 0;
                player.EnqueueToVisible(new ServerEntityGroupAssociation
                {
                    UnitId  = player.Guid,
                    GroupId = player.GroupAssociation
                }, true);

                return true;
            });
        }
    }
}
