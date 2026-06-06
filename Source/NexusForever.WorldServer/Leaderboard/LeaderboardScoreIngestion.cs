using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Leaderboard;
using NexusForever.WorldServer.Service;
using NLog;

namespace NexusForever.WorldServer.Leaderboard
{
    public sealed class LeaderboardScoreIngestion : ILeaderboardScoreIngestion
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly object scoreIdLock = new();
        private ulong nextPveScoreId;
        private ulong nextPvpScoreId;
        private bool pveScoreIdsInitialized;
        private bool pvpScoreIdsInitialized;

        private readonly IDatabaseManager databaseManager;
        private readonly IRealmContext realmContext;
        private readonly DatabaseLeaderboardStore store;
        private readonly IBackgroundTaskRunner backgroundTaskRunner;

        public LeaderboardScoreIngestion(
            IDatabaseManager databaseManager,
            IRealmContext realmContext,
            DatabaseLeaderboardStore store,
            IBackgroundTaskRunner backgroundTaskRunner)
        {
            this.databaseManager       = databaseManager;
            this.realmContext          = realmContext;
            this.store                 = store;
            this.backgroundTaskRunner  = backgroundTaskRunner;

            CharacterDatabase database = databaseManager.GetDatabase<CharacterDatabase>();
            if (database == null)
                return;

            nextPveScoreId         = database.GetNextLeaderboardPveScoreId() + 1ul;
            nextPvpScoreId         = database.GetNextLeaderboardPvpScoreId() + 1ul;
            pveScoreIdsInitialized = true;
            pvpScoreIdsInitialized = true;
        }

        public void RecordPveCompletion(
            IPlayer player,
            LeaderboardType type,
            uint matchingGameMapId,
            uint primeLevel,
            uint completionTime,
            uint rewardedTier,
            IReadOnlyList<LeaderboardTeamMemberRecord> teamMembers = null)
        {
            if (player == null || completionTime == 0u)
                return;

            var database = databaseManager.GetDatabase<CharacterDatabase>();
            if (database == null)
            {
                log.Warn("RecordPveCompletion skipped: CharacterDatabase is unavailable.");
                return;
            }

            ulong scoreId = AllocatePveScoreId(database);

            var model = new LeaderboardPveScoreModel
            {
                Id                = scoreId,
                CharacterId       = player.CharacterId,
                RealmId           = realmContext.RealmId,
                Type              = (byte)type,
                MatchingGameMapId = matchingGameMapId,
                PrimeLevel        = primeLevel,
                CompletionTime    = completionTime,
                RewardedTier      = rewardedTier,
                PlayerName        = player.Name ?? string.Empty,
                PlayerClass       = (byte)player.Class,
                GuildId           = player.GuildManager.Guild?.Id ?? 0ul,
                TeamMembersJson   = SerializeTeamMembers(teamMembers),
                RecordedUtc       = DateTime.UtcNow
            };

            backgroundTaskRunner.Observe(
                database.Save(context => context.LeaderboardPveScore.Add(model)),
                store.InvalidateCache,
                exception => log.Error(exception, "Failed to persist PvE leaderboard score for character {0}.", player.CharacterId));
        }

        public void RecordPvpRating(
            IPlayer player,
            LeaderboardType type,
            uint rating,
            IReadOnlyList<LeaderboardTeamMemberRecord> teamMembers = null)
        {
            if (player == null || rating == 0u)
                return;

            var database = databaseManager.GetDatabase<CharacterDatabase>();
            if (database == null)
            {
                log.Warn("RecordPvpRating skipped: CharacterDatabase is unavailable.");
                return;
            }

            ulong scoreId = AllocatePvpScoreId(database);

            var model = new LeaderboardPvpScoreModel
            {
                Id              = scoreId,
                CharacterId     = player.CharacterId,
                RealmId         = realmContext.RealmId,
                Type            = (byte)type,
                Rating          = rating,
                PlayerName      = player.Name ?? string.Empty,
                PlayerClass     = (byte)player.Class,
                GuildId         = player.GuildManager.Guild?.Id ?? 0ul,
                TeamMembersJson = SerializeTeamMembers(teamMembers),
                RecordedUtc     = DateTime.UtcNow
            };

            backgroundTaskRunner.Observe(
                database.Save(context => context.LeaderboardPvpScore.Add(model)),
                store.InvalidateCache,
                exception => log.Error(exception, "Failed to persist PvP leaderboard score for character {0}.", player.CharacterId));
        }

        private ulong AllocatePveScoreId(CharacterDatabase database)
        {
            lock (scoreIdLock)
            {
                if (!pveScoreIdsInitialized)
                {
                    nextPveScoreId        = database.GetNextLeaderboardPveScoreId() + 1ul;
                    pveScoreIdsInitialized = true;
                }

                return nextPveScoreId++;
            }
        }

        private ulong AllocatePvpScoreId(CharacterDatabase database)
        {
            lock (scoreIdLock)
            {
                if (!pvpScoreIdsInitialized)
                {
                    nextPvpScoreId        = database.GetNextLeaderboardPvpScoreId() + 1ul;
                    pvpScoreIdsInitialized = true;
                }

                return nextPvpScoreId++;
            }
        }

        private static string SerializeTeamMembers(IReadOnlyList<LeaderboardTeamMemberRecord> teamMembers)
        {
            if (teamMembers == null || teamMembers.Count == 0)
                return null;

            return JsonSerializer.Serialize(teamMembers);
        }
    }
}
