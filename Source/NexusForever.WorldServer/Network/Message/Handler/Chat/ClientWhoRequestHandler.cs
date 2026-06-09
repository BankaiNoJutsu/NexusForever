using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Who;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Who;
using NexusForever.Network.World.Message.Model.Who.Parameter;
using PlayerPath = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.WorldServer.Network.Message.Handler.Chat
{
    public class ClientWhoRequestHandler : IMessageHandler<IWorldSession, ClientWhoRequest>
    {
        #region Dependency Injection

        private readonly IPlayerManager playerManager;
        private readonly IRealmContext realmContext;

        public ClientWhoRequestHandler(
            IPlayerManager playerManager,
            IRealmContext realmContext)
        {
            this.playerManager = playerManager;
            this.realmContext  = realmContext;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientWhoRequest request)
        {
            List<ServerWhoResponse.WhoPlayer> players = GetMatchingPlayers(session.Player, request)
                .OrderBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                .Select(BuildWhoPlayer)
                .ToList();

            session.EnqueueMessageEncrypted(new ServerWhoResponse
            {
                Players = players,
                Result  = WhoResult.OK
            });
        }

        private IEnumerable<IPlayer> GetMatchingPlayers(IPlayer source, ClientWhoRequest request)
        {
            IEnumerable<IPlayer> players = playerManager.Where(player => player != null);

            if (request.Parameters.Count == 0)
            {
                uint? zoneId = source?.Zone?.Id;
                return players.Where(player =>
                    player.CharacterId != source?.CharacterId &&
                    player.Zone?.Id == zoneId);
            }

            IReadOnlyList<IReadOnlyList<WhoParameter>> groups = GetParameterGroups(request);
            return players.Where(player => groups.Any(group => group.All(parameter => MatchesParameter(player, parameter))));
        }

        private static IReadOnlyList<IReadOnlyList<WhoParameter>> GetParameterGroups(ClientWhoRequest request)
        {
            var groups = new List<IReadOnlyList<WhoParameter>>();
            int index = 0;

            foreach (int groupCount in request.ParameterGroupCounts)
            {
                if (groupCount <= index)
                    continue;

                int groupEnd = Math.Min(groupCount, request.Parameters.Count);
                if (groupEnd <= index)
                    break;

                groups.Add(request.Parameters.Skip(index).Take(groupEnd - index).ToArray());
                index = groupEnd;
            }

            if (index < request.Parameters.Count)
                groups.Add(request.Parameters.Skip(index).ToArray());

            return groups;
        }

        private bool MatchesParameter(IPlayer player, WhoParameter parameter)
        {
            return parameter.Data switch
            {
                WhoParameterLevel level       => MatchesLevel(player, level),
                WhoParameterRace race         => player.Race == race.RaceId,
                WhoParameterPath path         => player.Path == path.PathId,
                WhoParameterClass playerClass => player.Class == playerClass.ClassId,
                WhoParameterZone zone         => player.Zone?.Id == zone.WorldZoneId,
                WhoParameterGuild guild       => MatchesGuild(player, guild.GuildName),
                WhoParameterPlayer playerName => MatchesSearchText(player, playerName.PlayerName),
                WhoParameterCombo combo       => MatchesCombo(player, combo),
                WhoParameterFaction faction   => player.Faction1 == faction.Faction2Id,
                _                             => false
            };
        }

        private static bool MatchesLevel(IPlayer player, WhoParameterLevel level)
        {
            return player.Level >= level.BottomLevel && player.Level < level.TopLevel;
        }

        private static bool MatchesCombo(IPlayer player, WhoParameterCombo combo)
        {
            if (combo.RaceId != Race.None && player.Race != combo.RaceId)
                return false;

            if (combo.PathId != PlayerPath.None && player.Path != combo.PathId)
                return false;

            if (combo.ClassId != Class.None && player.Class != combo.ClassId)
                return false;

            if (combo.WorldZoneId != 0u && player.Zone?.Id != combo.WorldZoneId)
                return false;

            return MatchesSearchText(player, combo.SearchString);
        }

        private static bool MatchesSearchText(IPlayer player, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            return Contains(player.Name, searchText)
                || Contains(player.GuildManager?.GuildAffiliation?.Name, searchText);
        }

        private static bool MatchesGuild(IPlayer player, string guildName)
        {
            if (string.IsNullOrWhiteSpace(guildName))
                return true;

            IGuildBase guild = player.GuildManager?.GuildAffiliation;
            return Contains(guild?.Name, guildName);
        }

        private static bool Contains(string value, string searchText)
        {
            return value?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;
        }

        private ServerWhoResponse.WhoPlayer BuildWhoPlayer(IPlayer player)
        {
            return new ServerWhoResponse.WhoPlayer
            {
                Name    = player.Name,
                Realm   = realmContext.RealmName,
                Level   = player.Level,
                Race    = player.Race,
                Class   = player.Class,
                Path    = player.Path,
                Faction = player.Faction1,
                Sex     = player.Sex,
                Zone    = player.Zone?.Id ?? 0u
            };
        }
    }
}
