using NexusForever.GameTable.Model;

namespace NexusForever.GameTable.Text.Filter
{
    public static class TextFilterGameTableExtensions
    {
        public static async Task InitialiseWordFilterAsync(this IGameTableManager gameTableManager, ITextFilterManager textFilterManager)
        {
            var loader = new GameTableLoader()
                .AddGameTable<WordFilterEntry>();

            await gameTableManager.Initialise(loader);
            textFilterManager.Initialise();
        }
    }
}
