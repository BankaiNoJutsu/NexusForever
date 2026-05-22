using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NexusForever.Database;
using NexusForever.Game.Abstract;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    public sealed class DatabaseLeaderboardStore : ILeaderboardStore
    {
        private const int MaxRowsPerScope = 100;

        private readonly IDatabaseManager databaseManager;
        private readonly IRealmContext realmContext;
        private readonly object sync = new();
        private IReadOnlyList<LeaderboardPveEntryRecord> cachedPve;
        private IReadOnlyList<LeaderboardPvpEntryRecord> cachedPvp;

        public DatabaseLeaderboardStore(IDatabaseManager databaseManager, IRealmContext realmContext)
        {
            this.databaseManager = databaseManager;
            this.realmContext    = realmContext;
        }

        public void InvalidateCache()
        {
            lock (sync)
            {
                cachedPve = null;
                cachedPvp = null;
            }
        }

        public IReadOnlyList<LeaderboardPveEntryRecord> GetPveEntries(LeaderboardType type, uint matchingGameMapId, uint primeLevel)
        {
            EnsureLoaded();
            return cachedPve
                .Where(entry => LeaderboardCategoryRules.MatchesPveScope(entry, type, matchingGameMapId, primeLevel))
                .ToList();
        }

        public IReadOnlyList<LeaderboardPvpEntryRecord> GetPvpEntries(LeaderboardType type)
        {
            EnsureLoaded();
            return cachedPvp
                .Where(entry => LeaderboardCategoryRules.MatchesPvpCategory(entry, type))
                .ToList();
        }

        private void EnsureLoaded()
        {
            lock (sync)
            {
                if (cachedPve != null && cachedPvp != null)
                    return;

                var database = databaseManager.GetDatabase<CharacterDatabase>();

                cachedPve = database.GetLeaderboardPveScores(realmContext.RealmId)
                    .OrderBy(s => s.CompletionTime)
                    .Take(MaxRowsPerScope * 8)
                    .Select(MapPve)
                    .ToList();

                cachedPvp = database.GetLeaderboardPvpScores(realmContext.RealmId)
                    .OrderByDescending(s => s.Rating)
                    .Take(MaxRowsPerScope * 8)
                    .Select(MapPvp)
                    .ToList();
            }
        }

        private static LeaderboardPveEntryRecord MapPve(LeaderboardPveScoreModel model)
        {
            return new LeaderboardPveEntryRecord
            {
                CharacterId       = model.CharacterId,
                Name              = model.PlayerName,
                Class             = (Class)model.PlayerClass,
                GuildId           = model.GuildId,
                Type              = (LeaderboardType)model.Type,
                MatchingGameMapId = model.MatchingGameMapId,
                PrimeLevel        = model.PrimeLevel,
                RewardedTier      = model.RewardedTier,
                CompletionTime    = model.CompletionTime,
                LastRank          = 0u,
                TeamMembers       = DeserializeTeamMembers(model.TeamMembersJson)
            };
        }

        private static LeaderboardPvpEntryRecord MapPvp(LeaderboardPvpScoreModel model)
        {
            return new LeaderboardPvpEntryRecord
            {
                CharacterId = model.CharacterId,
                Name        = model.PlayerName,
                Class       = (Class)model.PlayerClass,
                GuildId     = model.GuildId,
                Type        = (LeaderboardType)model.Type,
                Rating      = model.Rating,
                LastRank    = 0u,
                TeamMembers = DeserializeTeamMembers(model.TeamMembersJson)
            };
        }

        private static IReadOnlyList<LeaderboardTeamMemberRecord> DeserializeTeamMembers(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return [];

            try
            {
                return JsonSerializer.Deserialize<List<LeaderboardTeamMemberRecord>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }
}
