using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
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
            return (Volatile.Read(ref cachedPve) ?? Array.Empty<LeaderboardPveEntryRecord>())
                .Where(entry => LeaderboardCategoryRules.MatchesPveScope(entry, type, matchingGameMapId, primeLevel))
                .ToList();
        }

        public IReadOnlyList<LeaderboardPvpEntryRecord> GetPvpEntries(LeaderboardType type)
        {
            EnsureLoaded();
            return (Volatile.Read(ref cachedPvp) ?? Array.Empty<LeaderboardPvpEntryRecord>())
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
                    return;

                IReadOnlyList<LeaderboardPveEntryRecord> loadedPve = LimitPveRowsPerScope(
                    database.GetLeaderboardPveScores(realmContext.RealmId).Select(MapPve));

                IReadOnlyList<LeaderboardPvpEntryRecord> loadedPvp = LimitPvpRowsPerCategory(
                    database.GetLeaderboardPvpScores(realmContext.RealmId).Select(MapPvp));

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

        internal static IReadOnlyList<LeaderboardPveEntryRecord> LimitPveRowsPerScope(IEnumerable<LeaderboardPveEntryRecord> entries)
        {
            return entries
                .Where(entry => LeaderboardCategoryRules.IsPveType(entry.Type))
                .GroupBy(entry => new { entry.Type, entry.MatchingGameMapId, entry.PrimeLevel })
                .SelectMany(group => group
                    .OrderBy(entry => entry.CompletionTime)
                    .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                    .ThenBy(entry => entry.CharacterId)
                    .GroupBy(entry => entry.CharacterId)
                    .Select(characterGroup => characterGroup.First())
                    .Take(MaxRowsPerScope))
                .ToList();
        }

        internal static IReadOnlyList<LeaderboardPvpEntryRecord> LimitPvpRowsPerCategory(IEnumerable<LeaderboardPvpEntryRecord> entries)
        {
            return entries
                .Where(entry => LeaderboardCategoryRules.MatchesPvpCategory(entry, entry.Type))
                .GroupBy(entry => entry.Type)
                .SelectMany(group => group
                    .OrderByDescending(entry => entry.Rating)
                    .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                    .ThenBy(entry => entry.CharacterId)
                    .GroupBy(entry => entry.CharacterId)
                    .Select(characterGroup => characterGroup.First())
                    .Take(MaxRowsPerScope))
                .ToList();
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
