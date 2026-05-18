using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Friendship;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Network.World.Message.Model.Friendship;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Friendship
{
    public class FriendshipTypeUpdatedHandler : IHandleMessages<FriendshipTypeUpdatedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;

        public FriendshipTypeUpdatedHandler(
            IPlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        #endregion

        public Task Handle(FriendshipTypeUpdatedMessage message)
        {
            IPlayer player = playerManager.GetPlayer(message.Friend.InviterCharacter.Identity.ToGameIdentity());
            if (message.Friend.Type == FriendshipType.FriendAndRival)
                player.AchievementManager.CheckAchievements(player, AchievementType.FriendAdd, 0u);

            player.Session.EnqueueMessageEncrypted(new ServerFriendshipTypeUpdate
            {
                FriendshipId = message.Friend.Id,
                Type         = message.Friend.Type
            });

            return Task.CompletedTask;
        }
    }
}
