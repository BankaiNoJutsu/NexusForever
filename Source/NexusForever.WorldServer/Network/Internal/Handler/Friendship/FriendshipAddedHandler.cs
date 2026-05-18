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
    public class FriendshipAddedHandler : IHandleMessages<FriendshipAddedMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;

        public FriendshipAddedHandler(
            IPlayerManager playerManager)
        {
            this.playerManager = playerManager;
        }

        #endregion

        public Task Handle(FriendshipAddedMessage message)
        {
            IPlayer player = playerManager.GetPlayer(message.Friend.InviterCharacter.Identity.ToGameIdentity());
            if (message.Friend.Type == FriendshipType.Friend)
                player.AchievementManager.CheckAchievements(player, AchievementType.FriendAdd, 0u);

            player.Session.EnqueueMessageEncrypted(new ServerFriendshipAdd
            {
                Friend = new FriendData
                {
                    FriendshipId   = message.Friend.Id,
                    PlayerIdentity = message.Friend.InviteeCharacter.Identity.ToNetworkIdentity(),
                    Note           = message.Friend.Note,
                    Type           = message.Friend.Type
                }
            });

            return Task.CompletedTask;
        }
    }
}
