using NexusForever.Database.Query.Repository.Query.Parameter;
using NexusForever.Network.Internal.Message.Who.Parameter;

namespace NexusForever.Server.Character.Game.Who
{
    public static class WhoParameterMappingExtensions
    {
        public static QueryParameterClass ToQueryParameter(this WhoParameterClass @class)
        {
            return new QueryParameterClass
            {
                Class = @class.ClassId
            };
        }

        public static QueryParameterFaction ToQueryParameter(this WhoParameterFaction faction)
        {
            return new QueryParameterFaction
            {
                Faction = faction.FactionId
            };
        }

        public static QueryParameterGuild ToQueryParameter(this WhoParameterGuild guild)
        {
            return new QueryParameterGuild
            {
                GuildName = guild.GuildName
            };
        }

        public static QueryParameterLevel ToQueryParameter(this WhoParameterLevel level)
        {
            return new QueryParameterLevel
            {
                BottomLevel = level.BottomLevel,
                TopLevel    = level.TopLevel,
            };
        }

        public static QueryParameterPath ToQueryPararmeter(this WhoParameterPath path)
        {
            return new QueryParameterPath
            {
                Path = path.PathId
            };
        }

        public static QueryParameterPlayer ToQueryParameter(this WhoParameterPlayer name)
        {
            return new QueryParameterPlayer
            {
                Name = name.PlayerName
            };
        }

        public static QueryParameterRace ToQueryParameter(this WhoParameterRace race)
        {
            return new QueryParameterRace
            {
                Race = race.RaceId
            };
        }

        public static QueryParameterWorldZone ToQueryParameter(this WhoParameterZone zone)
        {
            return new QueryParameterWorldZone
            {
                WorldZoneId = zone.WorldZoneId
            };
        }

        public static QueryParameterCombo ToQueryParameter(this WhoParameterCombo combo)
        {
            return new QueryParameterCombo
            {
                SearchString = combo.SearchString,
                Race         = combo.RaceId,
                Path         = combo.PathId,
                Class        = combo.ClassId,
                WorldZoneId  = combo.WorldZoneId,
                WorldZoneId2 = combo.WorldZoneId2
            };
        }
    }
}
