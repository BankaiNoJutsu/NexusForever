using System.Linq.Expressions;
using NexusForever.Database.Query.Model;

namespace NexusForever.Database.Query.Repository.Query.Parameter
{
    public class QueryParameterGuild : IQueryParameter
    {
        public string GuildName { get; set; }

        public Expression<Func<CharacterModel, bool>> Express()
        {
            if (string.IsNullOrWhiteSpace(GuildName))
                return _ => true;

            return c => c.GuildName != null && c.GuildName.Contains(GuildName);
        }
    }
}
