using NexusForever.Game.Abstract.Chat.Format;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.Internal.Message.Chat.Shared.Format;
using NexusForever.Network.Internal.Message.Chat.Shared.Format.Model;
using NexusForever.Network.World.Chat;
using NexusForever.Network.World.Chat.Model;

namespace NexusForever.Game.Chat.Format.Formatter
{
    public class ItemGuidChatFormatter : IInternalChatFormatter<ChatFormatItemGuid>, ILocalChatFormatter<ChatFormatItemGuid>
    {
        public IChatChannelTextFormatModel ToInternal(IPlayer player, ChatFormatItemGuid format)
        {
            IItem item = player.Inventory.GetItem(format.ItemGuid);

            return BuildInternalItemFull(item);
        }

        public IChatFormatModel ToLocal(IPlayer player, ChatFormatItemGuid format)
        {
            IItem item = player.Inventory.GetItem(format.ItemGuid);

            return BuildLocalItemFull(item);
        }

        private static ChatChannelTextItemFullFormat BuildInternalItemFull(IItem item)
        {
            return new ChatChannelTextItemFullFormat
            {
                ItemGuid = item.Guid,
                Item2Id  = item.Id
            };
        }

        private static ChatFormatItemFull BuildLocalItemFull(IItem item)
        {
            return new ChatFormatItemFull
            {
                ItemGuid = item.Guid,
                Item2Id  = item.Id
            };
        }
    }
}
