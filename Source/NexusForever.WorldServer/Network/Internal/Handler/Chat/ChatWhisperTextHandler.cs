using System.Linq;
using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Chat.Format;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Chat;
using NexusForever.Game.Static.Chat;
using NexusForever.Network.Internal;
using NexusForever.Network.Internal.Message.Chat;
using NexusForever.Network.Internal.Message.Chat.Shared;
using NexusForever.Shared;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Chat
{
    public class ChatWhisperTextHandler : IHandleMessages<ChatWhisperTextMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;
        private readonly IChatFormatManager chatFormatManager;
        private readonly IInternalMessagePublisher messagePublisher;

        public ChatWhisperTextHandler(
            IPlayerManager playerManager,
            IChatFormatManager chatFormatManager,
            IInternalMessagePublisher messagePublisher)
        {
            this.playerManager     = playerManager;
            this.chatFormatManager = chatFormatManager;
            this.messagePublisher  = messagePublisher;
        }

        #endregion

        public Task Handle(ChatWhisperTextMessage message)
        {
            var recipient = IncomingChatRecipient.Capture(playerManager, message.Recipient.ToGameIdentity());
            if (recipient == null)
                return Task.CompletedTask;

            // The routing records and strings are immutable values; do not retain
            // the mutable broker envelope across the World queue boundary.
            var sender = message.Sender; var senderName = message.SenderName;
            var recipientIdentity = message.Recipient; var recipientName = message.RecipientName;
            bool accountWhisper = message.IsAccountWhisper, automatic = message.AutoResponse;

            var builder = new ChatMessageBuilder();
            builder.Type    = accountWhisper ? ChatChannelType.AccountWhisper : ChatChannelType.Whisper;
            builder.Text    = message.Text.Text;
            builder.Formats = chatFormatManager.ToNetwork(message.Text.Format).ToList();
            builder.AutoResponse = automatic;

            builder.FromName = senderName.Name;
            if (recipientIdentity.RealmId != sender.RealmId)
                builder.FromRealm = senderName.RealmName;

            recipient.Enqueue(() =>
            {
                if (!recipient.Send(builder.Build()) || automatic) return;
                IPlayer player = recipient.Player;
                string text = player.PresenceState switch
                {
                    AccountPresenceState.Away => player.AwayAutoResponseMessage,
                    AccountPresenceState.Busy => player.BusyAutoResponseMessage,
                    _ => null
                };
                if (string.IsNullOrWhiteSpace(text) || !recipient.IsCurrent) return;

                messagePublisher.PublishAsync(new ChatWhisperTextMessage
                {
                    Sender           = recipientIdentity,
                    SenderName       = recipientName,
                    Recipient        = sender,
                    RecipientName    = senderName,
                    Text             = new ChatChannelText { Text = text },
                    IsAccountWhisper = accountWhisper,
                    AutoResponse     = true
                }).FireAndForgetAsync();
            });

            return Task.CompletedTask;
        }
    }
}
