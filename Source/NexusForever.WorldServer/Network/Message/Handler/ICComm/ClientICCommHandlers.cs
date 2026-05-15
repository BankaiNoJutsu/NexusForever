using System;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.ICComm;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.ICComm;

namespace NexusForever.WorldServer.Network.Message.Handler.ICComm
{
    public class ClientICCommChannelJoinHandler : IMessageHandler<IWorldSession, ClientICCommChannelJoin>
    {
        #region Dependency Injection

        private readonly ILogger<ClientICCommChannelJoinHandler> log;

        public ClientICCommChannelJoinHandler(
            ILogger<ClientICCommChannelJoinHandler> log)
        {
            this.log = log;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientICCommChannelJoin message)
        {
            if (!ICCommPacketValidation.IsDefined(message.Type))
                throw new InvalidPacketValueException();

            log.LogDebug("Rejecting unsupported ICComm channel join from player {PlayerGuid}: type {Type}, guild {GuildId}, name length {NameLength}.",
                session.Player?.Guid, message.Type, message.GuildId, message.Name?.Length ?? 0);

            session.EnqueueMessageEncrypted(new ServerICCommChannelJoinResult
            {
                Type        = message.Type,
                Result      = ICCommJoinResult.MissingEntitlement,
                ChannelName = message.Name ?? string.Empty
            });
        }
    }

    public class ClientICCommMessageHandler : IMessageHandler<IWorldSession, ClientICCommMessage>
    {
        #region Dependency Injection

        private readonly ILogger<ClientICCommMessageHandler> log;

        public ClientICCommMessageHandler(
            ILogger<ClientICCommMessageHandler> log)
        {
            this.log = log;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientICCommMessage message)
        {
            ICCommMessageResult result = string.IsNullOrWhiteSpace(message.Message)
                ? ICCommMessageResult.InvalidText
                : ICCommMessageResult.NotInChannel;

            log.LogDebug("Rejecting unsupported ICComm message from player {PlayerGuid}: channel {IccommId}, message id {MessageId}, message length {MessageLength}, recipient length {RecipientLength}, result {Result}.",
                session.Player?.Guid, message.IccommId, message.MessageId, message.Message?.Length ?? 0,
                message.RecipientName?.Length ?? 0, result);

            session.EnqueueMessageEncrypted(new ServerICCommMessageResult
            {
                IccomId   = message.IccommId,
                MessageId = message.MessageId,
                Result    = result
            });
        }
    }

    public class ClientICCommChannelNotJoinedHandler : IMessageHandler<IWorldSession, ClientICCommChannelNotJoined>
    {
        #region Dependency Injection

        private readonly ILogger<ClientICCommChannelNotJoinedHandler> log;

        public ClientICCommChannelNotJoinedHandler(
            ILogger<ClientICCommChannelNotJoinedHandler> log)
        {
            this.log = log;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientICCommChannelNotJoined message)
        {
            log.LogDebug("Received ICComm not-joined notice from player {PlayerGuid}: channel {IccomId}.",
                session.Player?.Guid, message.IccomId);
        }
    }

    internal static class ICCommPacketValidation
    {
        public static bool IsDefined<T>(T value) where T : struct, Enum
        {
            return Enum.IsDefined(typeof(T), value);
        }
    }
}
