using NexusForever.Game;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Friendship;
using NexusForever.Shared;

namespace NexusForever.WorldServer.Network.Message.Handler.Friendship
{
    public class ClientFriendshipAccountSetPublicDisplayNameHandler : IMessageHandler<IWorldSession, ClientFriendshipAccountSetPublicDisplayName>
    {
        #region Dependency Injection

        private readonly IInternalMessagePublisher messagePublisher;

        public ClientFriendshipAccountSetPublicDisplayNameHandler(
            IInternalMessagePublisher messagePublisher)
        {
            this.messagePublisher = messagePublisher;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientFriendshipAccountSetPublicDisplayName message)
        {
            if (!FriendshipAccountCommandSource.TryCapture(session, out var player, out var account, out var owner)) return;
            messagePublisher.PublishAsync(new FriendshipAccountNicknameUpdateMessage
            {
                Source          = player.Identity.ToInternalIdentity(),
                AccountId       = account.Id, PresenceOwner = owner,
                AccountNickname = message.PublicDisplayName
            }).FireAndForgetAsync();
        }
    }
}
