using NexusForever.Game.Static.Chat;
using NexusForever.Network;
using NexusForever.Network.World.Chat;

namespace NexusForever.Network.World.Message.Model.Chat
{
    public class ChatClientFormat : ChatFormat
    {
        #region Dependency Injection

        private readonly IChatFormatModelFactory chatFormatFactory;

        public ChatClientFormat(
            IChatFormatModelFactory chatFormatFactory)
        {
            this.chatFormatFactory = chatFormatFactory;
        }

        #endregion

        public void Read(GamePacketReader reader)
        {
            Type       = reader.ReadEnum<ChatFormatType>(4);
            StartIndex = reader.ReadUShort();
            StopIndex  = reader.ReadUShort();

            Model = chatFormatFactory.NewChatFormatModel(Type);
            if (Model == null)
                throw new InvalidPacketValueException($"Unsupported chat format type {Type}.");

            Model.Read(reader);
        }
    }
}
