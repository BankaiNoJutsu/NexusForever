using System;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.ICComm;
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
        private readonly IICCommManager icCommManager;

        public ClientICCommChannelJoinHandler(
            ILogger<ClientICCommChannelJoinHandler> log,
            IICCommManager icCommManager)
        {
            this.log           = log;
            this.icCommManager = icCommManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientICCommChannelJoin message)
        {
            if (!ICCommPacketValidation.IsDefined(message.Type))
                throw new InvalidPacketValueException();

            ICCommJoinResult? result = icCommManager.Join(session.Player, message.Type, message.GuildId, message.Name,
                out ulong iccommId, out string channelName);

            if (result.HasValue)
            {
                session.EnqueueMessageEncrypted(new ServerICCommChannelJoinResult
                {
                    Type        = message.Type,
                    Result      = result.Value,
                    ChannelName = channelName
                });
            }
            else
            {
                session.EnqueueMessageEncrypted(new ServerICCommChannelJoin
                {
                    Type        = message.Type,
                    IccomId     = iccommId,
                    ChannelName = channelName
                });
            }

            log.LogDebug("Processed ICComm channel join from player {PlayerGuid}: type {Type}, guild {GuildId}, name length {NameLength}, channel {IccommId}, result {Result}.",
                session.Player?.Guid, message.Type, message.GuildId, message.Name?.Length ?? 0, iccommId,
                result?.ToString() ?? "Joined");
        }
    }

    public class ClientICCommMessageHandler : IMessageHandler<IWorldSession, ClientICCommMessage>
    {
        #region Dependency Injection

        private readonly ILogger<ClientICCommMessageHandler> log;
        private readonly IICCommManager icCommManager;

        public ClientICCommMessageHandler(
            ILogger<ClientICCommMessageHandler> log,
            IICCommManager icCommManager)
        {
            this.log           = log;
            this.icCommManager = icCommManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientICCommMessage message)
        {
            ICCommMessageResult result = icCommManager.SendMessage(session.Player, message.IccommId, message.MessageId,
                message.Message, message.RecipientName);

            if (result != ICCommMessageResult.Sent)
            {
                session.EnqueueMessageEncrypted(new ServerICCommMessageResult
                {
                    IccomId   = message.IccommId,
                    MessageId = message.MessageId,
                    Result    = result
                });
            }

            log.LogDebug("Processed ICComm message from player {PlayerGuid}: channel {IccommId}, message id {MessageId}, message length {MessageLength}, recipient length {RecipientLength}, result {Result}.",
                session.Player?.Guid, message.IccommId, message.MessageId, message.Message?.Length ?? 0,
                message.RecipientName?.Length ?? 0, result);
        }
    }

    public class ClientICCommChannelNotJoinedHandler : IMessageHandler<IWorldSession, ClientICCommChannelNotJoined>
    {
        #region Dependency Injection

        private readonly ILogger<ClientICCommChannelNotJoinedHandler> log;
        private readonly IICCommManager icCommManager;

        public ClientICCommChannelNotJoinedHandler(
            ILogger<ClientICCommChannelNotJoinedHandler> log,
            IICCommManager icCommManager)
        {
            this.log           = log;
            this.icCommManager = icCommManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientICCommChannelNotJoined message)
        {
            bool removed = icCommManager.RemoveClientMembership(session.Player, message.IccomId);

            log.LogDebug("Received ICComm not-joined notice from player {PlayerGuid}: channel {IccomId}, removed {Removed}.",
                session.Player?.Guid, message.IccomId, removed);
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
