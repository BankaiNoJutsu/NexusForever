using Microsoft.Extensions.Logging;
using NexusForever.Game;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Friendship;
using NexusForever.Network.Internal.Message.Shared;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Friendship;
using NexusForever.Shared;
using NexusForever.WorldServer.Network.Internal;

namespace NexusForever.WorldServer.Network.Message.Handler.Friendship
{
    public class ClientFriendshipBlockHandler : IMessageHandler<IWorldSession, ClientFriendshipBlock>
    {
        #region Dependency Injection

        private readonly IInternalMessagePublisher messagePublisher;

        public ClientFriendshipBlockHandler(
            IInternalMessagePublisher messagePublisher)
        {
            this.messagePublisher = messagePublisher;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientFriendshipBlock message)
        {
            messagePublisher.PublishAsync(new FriendshipAccountBlockUpdateMessage
            {
                Identity            = session.Player.Identity.ToInternalIdentity(),
                AccountId           = session.Account.Id,
                BlockFriendRequests = message.BlockFriendRequests
            }).FireAndForgetAsync();
        }
    }

    public class ClientFriendshipIgnoreStrangersStateHandler : IMessageHandler<IWorldSession, ClientFriendshipIgnoreStrangersState>
    {
        #region Dependency Injection

        private readonly ILogger<ClientFriendshipIgnoreStrangersStateHandler> log;

        public ClientFriendshipIgnoreStrangersStateHandler(
            ILogger<ClientFriendshipIgnoreStrangersStateHandler> log)
        {
            this.log = log;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientFriendshipIgnoreStrangersState message)
        {
            log.LogDebug("Updated transient ignore-strangers state for player {PlayerGuid}: ignore {IgnoreStrangerInvites}.",
                session.Player?.Guid, message.IgnoreStrangerInvites);

            session.EnqueueMessageEncrypted(new ServerFriendshipIgnoreStrangersState
            {
                Flags = message.IgnoreStrangerInvites ? 2u : 0u
            });
        }
    }

    public class ClientFriendshipSetAutoResponseMessageHandler : IMessageHandler<IWorldSession, ClientFriendshipSetAutoResponseMessage>
    {
        #region Dependency Injection

        private readonly ILogger<ClientFriendshipSetAutoResponseMessageHandler> log;

        public ClientFriendshipSetAutoResponseMessageHandler(
            ILogger<ClientFriendshipSetAutoResponseMessageHandler> log)
        {
            this.log = log;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientFriendshipSetAutoResponseMessage message)
        {
            session.Player.AwayAutoResponseMessage = message.AwayMessage ?? string.Empty;
            session.Player.BusyAutoResponseMessage = message.BusyMessage ?? string.Empty;

            log.LogDebug("Updated auto-response messages for player {PlayerGuid}: away length {AwayLength}, busy length {BusyLength}.",
                session.Player?.Guid, message.AwayMessage?.Length ?? 0, message.BusyMessage?.Length ?? 0);
        }
    }

    public class ClientFriendshipRemoveByNameHandler : IMessageHandler<IWorldSession, ClientFriendshipRemoveByName>
    {
        #region Dependency Injection

        private readonly IInternalMessagePublisher messagePublisher;

        public ClientFriendshipRemoveByNameHandler(
            IInternalMessagePublisher messagePublisher)
        {
            this.messagePublisher = messagePublisher;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientFriendshipRemoveByName message)
        {
            messagePublisher.PublishAsync(new FriendshipRemoveNameMessage
            {
                Inviter = session.Player.Identity.ToInternalIdentity(),
                InviteeName = new IdentityName
                {
                    Name      = message.Name,
                    RealmName = message.RealmName
                },
                Type = message.Type
            }).FireAndForgetAsync();
        }
    }

    public class ClientFriendshipSetNoteByNameHandler : IMessageHandler<IWorldSession, ClientFriendshipSetNoteByName>
    {
        #region Dependency Injection

        private readonly IInternalMessagePublisher messagePublisher;

        public ClientFriendshipSetNoteByNameHandler(
            IInternalMessagePublisher messagePublisher)
        {
            this.messagePublisher = messagePublisher;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientFriendshipSetNoteByName message)
        {
            messagePublisher.PublishAsync(new FriendshipNoteUpdateByNameMessage
            {
                Source = session.Player.Identity.ToInternalIdentity(),
                TargetName = new IdentityName
                {
                    Name      = message.Name,
                    RealmName = message.RealmName
                },
                Note = message.Note
            }).FireAndForgetAsync();
        }
    }
}
