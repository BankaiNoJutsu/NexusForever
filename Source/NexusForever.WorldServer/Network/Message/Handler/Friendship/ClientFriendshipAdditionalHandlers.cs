using Microsoft.Extensions.Logging;
using NexusForever.Game;
using NexusForever.Game.Static.Entity;
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
        public void HandleMessage(IWorldSession session, ClientFriendshipBlock message)
        {
            var player = session?.Player;
            if (player == null || !ReferenceEquals(player.Session, session)) return;
            if (message.BlockFriendRequests) player.SetFlag(CharacterFlag.FriendBlock);
            else player.RemoveFlag(CharacterFlag.FriendBlock);
        }
    }

    public class ClientFriendshipIgnoreStrangersStateHandler : IMessageHandler<IWorldSession, ClientFriendshipIgnoreStrangersState>
    {
        #region Dependency Injection

        private readonly ILogger<ClientFriendshipIgnoreStrangersStateHandler> log;
        private readonly IInternalMessagePublisher messagePublisher;

        public ClientFriendshipIgnoreStrangersStateHandler(
            ILogger<ClientFriendshipIgnoreStrangersStateHandler> log,
            IInternalMessagePublisher messagePublisher)
        {
            this.log = log;
            this.messagePublisher = messagePublisher;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientFriendshipIgnoreStrangersState message)
        {
            if (!FriendshipAccountCommandSource.TryCapture(session, out var player, out var account, out var owner)) return;
            log.LogDebug("Updating account ignore-strangers state for player {PlayerGuid}: ignore {IgnoreStrangerInvites}.",
                session.Player?.Guid, message.IgnoreStrangerInvites);

            messagePublisher.PublishAsync(new FriendshipAccountBlockUpdateMessage
            {
                Identity = player.Identity.ToInternalIdentity(), AccountId = account.Id, PresenceOwner = owner,
                BlockFriendRequests = message.IgnoreStrangerInvites
            }).FireAndForgetAsync();
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
