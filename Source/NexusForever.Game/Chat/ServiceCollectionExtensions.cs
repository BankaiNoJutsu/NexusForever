using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Chat;
using NexusForever.Game.Chat.Format;

namespace NexusForever.Game.Chat
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameChat(this IServiceCollection sc)
        {
            sc.AddGameChatFormat();

            sc.AddSingleton<IGlobalChatManager, GlobalChatManager>();
        }
    }
}
