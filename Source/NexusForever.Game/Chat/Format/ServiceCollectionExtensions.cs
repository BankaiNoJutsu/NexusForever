using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Chat.Format;
using NexusForever.Game.Chat.Format.Formatter;
using NexusForever.Network.Internal.Message.Chat.Shared.Format.Model;
using NexusForever.Network.World.Chat.Model;

namespace NexusForever.Game.Chat.Format
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameChatFormat(this IServiceCollection sc)
        {
            sc.AddSingleton<IChatFormatManager, ChatFormatManager>();

            sc.AddTransient<IInternalChatFormatter<ChatFormat0>, Format0ChatFormatter>();
            sc.AddTransient<IInternalChatFormatter<ChatFormatAlien>, AlienChatFormatter>();
            sc.AddTransient<IInternalChatFormatter<ChatFormatRoleplay>, RoleplayChatFormatter>();
            sc.AddTransient<IInternalChatFormatter<ChatFormat3>, Format3ChatFormatter>();
            sc.AddTransient<IInternalChatFormatter<ChatFormatItemGuid>, ItemGuidChatFormatter>();
            sc.AddTransient<IInternalChatFormatter<ChatFormatItemId>, ItemIdChatFormatter>();
            sc.AddTransient<IInternalChatFormatter<ChatFormatQuestId>, QuestIdChatFormatter>();
            sc.AddTransient<IInternalChatFormatter<ChatFormatArchiveArticle>, ArchiveArticleChatFormatter>();
            sc.AddTransient<IInternalChatFormatter<ChatFormatProfanity>, ProfanityChatFormatter>();
            sc.AddTransient<IInternalChatFormatter<ChatFormatItemFull>, ItemFullChatFormatter>();
            sc.AddTransient<IInternalChatFormatter<ChatFormatNavPoint>, NavPointChatFormatter>();
            sc.AddTransient<IInternalChatFormatter<ChatFormatLoot>, LootChatFormatter>();

            sc.AddTransient<INetworkChatFormatter<ChatChannelTextFormat0Format>, Format0ChatFormatter>();
            sc.AddTransient<INetworkChatFormatter<ChatChannelTextAlienFormat>, AlienChatFormatter>();
            sc.AddTransient<INetworkChatFormatter<ChatChannelTextRoleplayFormat>, RoleplayChatFormatter>();
            sc.AddTransient<INetworkChatFormatter<ChatChannelTextFormat3Format>, Format3ChatFormatter>();
            sc.AddTransient<INetworkChatFormatter<ChatChannelTextItemIdFormat>, ItemIdChatFormatter>();
            sc.AddTransient<INetworkChatFormatter<ChatChannelTextQuestIdFormat>, QuestIdChatFormatter>();
            sc.AddTransient<INetworkChatFormatter<ChatChannelTextArchiveArticleFormat>, ArchiveArticleChatFormatter>();
            sc.AddTransient<INetworkChatFormatter<ChatChannelTextProfanityFormat>, ProfanityChatFormatter>();
            sc.AddTransient<INetworkChatFormatter<ChatChannelTextItemFullFormat>, ItemFullChatFormatter>();
            sc.AddTransient<INetworkChatFormatter<ChatChannelTextNavPointFormat>, NavPointChatFormatter>();
            sc.AddTransient<INetworkChatFormatter<ChatChannelTextLootFormat>, LootChatFormatter>();

            sc.AddTransient<ILocalChatFormatter<ChatFormatItemGuid>, ItemGuidChatFormatter>();
        }
    }
}
