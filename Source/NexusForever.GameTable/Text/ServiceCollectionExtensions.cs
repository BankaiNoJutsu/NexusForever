using Microsoft.Extensions.DependencyInjection;
using NexusForever.GameTable.Text.Filter;
using NexusForever.GameTable.Text.Search;

namespace NexusForever.GameTable.Text
{
    public static class ServiceCollectionExtensions
    {
        public static void AddGameTableText(this IServiceCollection sc)
        {
            sc.AddSingleton<ITextFilterManager, TextFilterManager>();
            sc.AddSingleton<ISearchManager, SearchManager>();
        }
    }
}
