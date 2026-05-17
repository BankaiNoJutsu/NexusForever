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
            IPlayer player = playerManager.GetPlayer(message.Recipient.ToGameIdentity());
            if (player == null)
                return Task.CompletedTask;

            var builder = new ChatMessageBuilder();
            builder.Type    = message.IsAccountWhisper ? ChatChannelType.AccountWhisper : ChatChannelType.Whisper;
            builder.Text    = message.Text.Text;
            builder.Formats = chatFormatManager.ToNetwork(message.Text.Format).ToList();
            builder.AutoResponse = message.AutoResponse;

            builder.FromName = message.SenderName.Name;
            if (message.Recipient.RealmId != message.Sender.RealmId)
                builder.FromRealm = message.SenderName.RealmName;

            player.Session.EnqueueMessageEncrypted(builder.Build());

            if (!message.AutoResponse)
                SendAutoResponse(player, message);

            return Task.CompletedTask;
        }

        private void SendAutoResponse(IPlayer player, ChatWhisperTextMessage message)
        {
            string text = player.PresenceState switch
            {
                AccountPresenceState.Away => player.AwayAutoResponseMessage,
                AccountPresenceState.Busy => player.BusyAutoResponseMessage,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(text))
                return;

            messagePublisher.PublishAsync(new ChatWhisperTextMessage
            {
                Sender           = message.Recipient,
                SenderName       = message.RecipientName,
                Recipient        = message.Sender,
                RecipientName    = message.SenderName,
                Text             = new ChatChannelText
                {
                    Text = text
                },
                IsAccountWhisper = message.IsAccountWhisper,
                AutoResponse     = true
            }).FireAndForgetAsync();
        }
    }
}
