using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public static class GuildLoadOrigin
    {
        private sealed record Origin(string Database);
        private static readonly ConditionalWeakTable<GuildModel, Origin> origins = new();
        public static string Get(GuildModel model) => origins.TryGetValue(model, out Origin origin) ? origin.Database : null;
        internal static void Bind(CharacterContext context, IEnumerable<GuildModel> models)
        {
            string database = CharacterDatabaseIdentity.Get(context);
            foreach (GuildModel model in models)
                origins.Add(model, new Origin(database));
        }
    }
}
