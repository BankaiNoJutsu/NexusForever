using System.Linq.Expressions;
using NexusForever.Database.Query.Model;

namespace NexusForever.Database.Query.Repository.Query.Parameter
{
    public class QueryParameterPlayer : IQueryParameter
    {
        public string Name { get; set; }

        public Expression<Func<CharacterModel, bool>> Express()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return _ => true;

            return c => (c.Name != null && c.Name.Contains(Name))
                || (c.GuildName != null && c.GuildName.Contains(Name));
        }
    }
}
