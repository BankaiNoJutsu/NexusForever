using System.Linq;
using System.Threading.Tasks;
using NexusForever.Game;
using NexusForever.Game.Abstract.Chat.Format;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Chat;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Static;
using NexusForever.Network.Internal.Message.Chat;
using Rebus.Handlers;

namespace NexusForever.WorldServer.Network.Internal.Handler.Chat
{
    public class ChatChannelTextHandler : IHandleMessages<ChatChannelTextMessage>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;
        private readonly IChatFormatManager chatFormatManager;
        private readonly IGameTableManager gameTableManager;

        public ChatChannelTextHandler(
            IPlayerManager playerManager,
            IChatFormatManager chatFormatManager,
            IGameTableManager gameTableManager)
        {
            this.playerManager     = playerManager;
            this.chatFormatManager = chatFormatManager;
            this.gameTableManager  = gameTableManager;
        }

        #endregion

        public Task Handle(ChatChannelTextMessage message)
        {
            ChatChannelEntry entry = gameTableManager.ChatChannel.GetEntry((ulong)message.ChatChannel.Type);
            if (entry == null)
                return Task.CompletedTask;

            var senderIdentity = message.Sender.Identity;
            var senderName = message.Sender.Character.IdentityName;
            var recipients = message.ChatChannel.Members.Select(member => member.Identity).ToArray();
            var type = message.ChatChannel.Type;
            ulong chatId = (entry.Flags & ChatChannelEntryFlags.ServerSide) != 0 ? message.ChatChannel.ChatId : 0;
            bool bubble = (entry.Flags & ChatChannelEntryFlags.ShowChatBubble) != 0;
            string text = message.Text.Text;

            foreach (var identity in recipients)
            {
                if (identity == senderIdentity)
                    continue;

                var recipient = IncomingChatRecipient.Capture(playerManager, identity.ToGameIdentity());
                if (recipient == null)
                    continue;

                var builder = new ChatMessageBuilder();
                builder.Type = type;
                builder.ChatId = chatId;
                
                builder.Text    = text;
                builder.Formats = chatFormatManager.ToNetwork(message.Text.Format).ToList();

                builder.FromName = senderName.Name;
                if (identity.RealmId != senderIdentity.RealmId)
                    builder.FromRealm = senderName.RealmName;

                recipient.Enqueue(() =>
                {
                    if (bubble)
                    {
                        IPlayer sender = recipient.Player.GetVisiblePlayer(senderIdentity.ToGameIdentity());
                        if (sender != null)
                            builder.Guid = sender.Guid;
                    }
                    recipient.Send(builder.Build());
                });
            }
            return Task.CompletedTask;
        }
    }
}
