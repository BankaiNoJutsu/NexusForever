using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Quest;

namespace NexusForever.Game.Quest
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameQuest(this IServiceCollection sc)
        {
            sc.AddSingleton<IGlobalQuestManager, GlobalQuestManager>();
            sc.AddSingleton<IContractManager, ContractManager>();
        }
    }
}
