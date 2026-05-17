using NexusForever.Database.Chat;
using NexusForever.Game.Static.Chat;
using NexusForever.Network.Internal.Message.Guild;
using NexusForever.Server.ChatServer.Chat;
using Rebus.Handlers;

namespace NexusForever.Server.ChatServer.Network.Internal.Handler.Guild
{
    public class GuildDisbandedHandler : IHandleMessages<GuildDisbandedMessage>
    {
        #region Dependency Injection

        private readonly ChatContext _chatContext;
        private readonly ChatChannelManager _chatChannelManager;

        public GuildDisbandedHandler(
            ChatContext chatContext,
            ChatChannelManager chatChannelManager)
        {
            _chatContext        = chatContext;
            _chatChannelManager = chatChannelManager;
        }

        #endregion

        public async Task Handle(GuildDisbandedMessage message)
        {
            await foreach (ChatChannel channel in _chatChannelManager.GetChatChannelsAsync(ChatChannelReferenceType.Guild, message.GuildId))
                _chatChannelManager.RemoveChatChannel(channel);

            await _chatContext.SaveChangesAsync();
        }
    }
}
