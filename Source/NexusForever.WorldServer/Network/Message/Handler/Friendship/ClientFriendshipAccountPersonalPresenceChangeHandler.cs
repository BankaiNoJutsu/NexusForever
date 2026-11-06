using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Friendship;
using NexusForever.Shared;

namespace NexusForever.WorldServer.Network.Message.Handler.Friendship
{
    public class ClientFriendshipAccountPersonalPresenceChangeHandler : IMessageHandler<IWorldSession, ClientFriendshipAccountPersonalPresenceChange>
    {
        #region Dependency Injection

        private readonly IInternalMessagePublisher messagePublisher;

        public ClientFriendshipAccountPersonalPresenceChangeHandler(
            IInternalMessagePublisher messagePublisher)
        {
            this.messagePublisher = messagePublisher;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientFriendshipAccountPersonalPresenceChange message)
        {
            if (!FriendshipAccountCommandSource.TryCapture(session, out var player, out var account, out var owner)) return;
            player.PresenceState = message.Presence;

            messagePublisher.PublishAsync(new FriendshipAccountPresenceUpdateMessage
            {
                AccountId = account.Id, PresenceOwner = owner,
                Presence  = message.Presence,
            }).FireAndForgetAsync();
        }
    }
}
