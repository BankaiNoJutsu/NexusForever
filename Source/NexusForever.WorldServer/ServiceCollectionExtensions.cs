using Microsoft.Extensions.DependencyInjection;
using NexusForever.WorldServer.Account;
using NexusForever.WorldServer.Command;
using NexusForever.WorldServer.Crafting;
using NexusForever.WorldServer.Leaderboard;
using NexusForever.WorldServer.Network.Message.Handler.Marketplace;
using NexusForever.WorldServer.Network.Message.Handler.Fortune;
using NexusForever.WorldServer.Service;
using NexusForever.WorldServer.Support;

namespace NexusForever.WorldServer
{
    public static class ServiceCollectionExtensions
    {
        public static void AddWorld(this IServiceCollection sc)
        {
            sc.AddSingleton<ICommandManager, CommandManager>();
            sc.AddSingleton<ILoginQueueManager, LoginQueueManager>();
            sc.AddSingleton<IFortuneSessionManager, FortuneSessionManager>();
            sc.AddSingleton<DatabaseLeaderboardStore>();
            sc.AddSingleton<ILeaderboardStore>(sp => sp.GetRequiredService<DatabaseLeaderboardStore>());
            sc.AddSingleton<ILeaderboardProvider, LeaderboardProvider>();
            sc.AddSingleton<ILeaderboardScoreIngestion, LeaderboardScoreIngestion>();
            sc.AddSingleton<ICREDDExchangeService, CREDDExchangeService>();
            sc.AddSingleton<ICraftingModifierSessionStore, CraftingModifierSessionStore>();
            sc.AddSingleton<IBackgroundTaskRunner, BackgroundTaskRunner>();
            sc.AddSingleton<IStorefrontPurchaseService, StorefrontPurchaseService>();
            sc.AddSingleton<ISupportSubmissionStore, FileSupportSubmissionStore>();
            sc.AddTransient<MarketplaceRequestHelper>();
        }
    }
}
