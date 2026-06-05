using System.Collections.Generic;
using System.Threading;
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
        private readonly SemaphoreSlim loadGate = new(1, 1);
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
            if (Volatile.Read(ref cachedPve) != null && Volatile.Read(ref cachedPvp) != null)
                return;

            loadGate.Wait();
            try
            {
                if (cachedPve != null && cachedPvp != null)
                    return;

                CharacterDatabase database = databaseManager.GetDatabase<CharacterDatabase>();
                if (database == null)
                {
                    lock (sync)
                    {
                        cachedPve ??= [];
                        cachedPvp ??= [];
                    }

                    return;
                }

                IReadOnlyList<LeaderboardPveEntryRecord> loadedPve = database.GetLeaderboardPveScores(realmContext.RealmId)
                    .OrderBy(s => s.CompletionTime)
                    .Take(MaxRowsPerScope * 8)
                    .Select(MapPve)
                    .ToList();

                IReadOnlyList<LeaderboardPvpEntryRecord> loadedPvp = database.GetLeaderboardPvpScores(realmContext.RealmId)
                    .OrderByDescending(s => s.Rating)
                    .Take(MaxRowsPerScope * 8)
                    .Select(MapPvp)
                    .ToList();

                lock (sync)
                {
                    cachedPve ??= loadedPve;
                    cachedPvp ??= loadedPvp;
                }
            }
            finally
            {
                loadGate.Release();
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

        private static IReadOnlyList<LeaderboardTeamMemberRecord> DeserializeTeamMembers(string teamMembersJson)
        {
            if (string.IsNullOrWhiteSpace(teamMembersJson))
                return [];

            try
            {
                return JsonSerializer.Deserialize<List<LeaderboardTeamMemberRecord>>(teamMembersJson) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }
}
