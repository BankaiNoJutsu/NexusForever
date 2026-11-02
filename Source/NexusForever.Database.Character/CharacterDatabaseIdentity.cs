using System;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace NexusForever.Database.Character
{
    // Shared with existing Item/Quest ownership. Preserve the connection-option
    // identity; this is not a physical-server/path alias canonicalizer.
    public static class CharacterDatabaseIdentity
    {
        public static string Get(CharacterContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = context.Database.GetDbConnection().ConnectionString
            };
            builder.Remove("Password");
            builder.Remove("Pwd");
            var normalized = new DbConnectionStringBuilder();
            foreach (string key in builder.Keys.Cast<string>().OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
                normalized[key.ToLowerInvariant()] = builder[key];
            return context.Database.ProviderName + ":" + normalized.ConnectionString;
        }

        public static void Require(CharacterContext context, string expected)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(expected);
                if (Get(context) != expected)
                    throw new InvalidOperationException("Character save changed its retained database route.");
            }
            catch (Exception error) { context?.RejectSave(error); throw; }
        }
    }
}
