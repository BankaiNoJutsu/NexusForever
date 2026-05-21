using Microsoft.Extensions.DependencyInjection;
using NexusForever.Shared;
using NexusForever.WorldServer.Command;
using NexusForever.WorldServer.Leaderboard;
using NexusForever.WorldServer.Network.Message.Handler.Fortune;
using NexusForever.WorldServer.Support;

namespace NexusForever.WorldServer
{
    public static class ServiceCollectionExtensions
    {
        public static void AddWorld(this IServiceCollection sc)
        {
            sc.AddSingletonLegacy<ICommandManager, CommandManager>();
            sc.AddSingletonLegacy<ILoginQueueManager, LoginQueueManager>();
            sc.AddSingleton<IFortuneSessionManager, FortuneSessionManager>();
            sc.AddSingleton<ILeaderboardProvider, EmptyLeaderboardProvider>();
            sc.AddSingleton<ISupportSubmissionStore, FileSupportSubmissionStore>();
        }
    }
}
