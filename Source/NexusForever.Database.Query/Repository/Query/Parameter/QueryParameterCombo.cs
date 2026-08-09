using System.Linq.Expressions;
using NexusForever.Database.Query.Model;
using NexusForever.Game.Static.Entity;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Database.Query.Repository.Query.Parameter
{
    public class QueryParameterCombo : IQueryParameter
    {
        public string SearchString { get; set; }
        public Race? Race { get; set; }
        public Path? Path { get; set; }
        public Class? Class { get; set; }
        public ushort? WorldZoneId { get; set; }
        public ushort? WorldZoneId2 { get; set; }

        public Expression<Func<CharacterModel, bool>> Express()
        {
            return c =>
                (!Race.HasValue || c.Race == Race.Value)
                && (!Path.HasValue || c.Path == Path.Value)
                && (!Class.HasValue || c.Class == Class.Value)
                && ((!WorldZoneId.HasValue && !WorldZoneId2.HasValue)
                    || (WorldZoneId.HasValue && c.WorldZoneId == WorldZoneId.Value)
                    || (WorldZoneId2.HasValue && c.WorldZoneId == WorldZoneId2.Value))
                && (string.IsNullOrWhiteSpace(SearchString)
                    || (c.Name != null && c.Name.Contains(SearchString))
                    || (c.GuildName != null && c.GuildName.Contains(SearchString)));
        }
    }
}
