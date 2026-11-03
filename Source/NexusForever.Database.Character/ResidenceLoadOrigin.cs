using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NexusForever.Database.Character.Model;

namespace NexusForever.Database.Character
{
    public static class ResidenceLoadOrigin
    {
        private sealed record Origin(string Database);
        private static readonly ConditionalWeakTable<ResidenceModel, Origin> origins = new();
        public static string Get(ResidenceModel model) => origins.TryGetValue(model, out Origin origin) ? origin.Database : null;
        internal static void Bind(CharacterContext context, IEnumerable<ResidenceModel> models)
        {
            string database = CharacterDatabaseIdentity.Get(context);
            foreach (ResidenceModel model in models) origins.Add(model, new Origin(database));
        }
    }
}
