using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    public sealed class LeaderboardScoreIngestion : ILeaderboardScoreIngestion
    {
        private readonly IDatabaseManager databaseManager;
        private readonly IRealmContext realmContext;
        private readonly DatabaseLeaderboardStore store;

        public LeaderboardScoreIngestion(
            IDatabaseManager databaseManager,
            IRealmContext realmContext,
            DatabaseLeaderboardStore store)
        {
            this.databaseManager = databaseManager;
            this.realmContext    = realmContext;
            this.store           = store;
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
            ulong scoreId = database.GetNextLeaderboardPveScoreId() + 1ul;

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

            database.Save(context => context.LeaderboardPveScore.Add(model)).GetAwaiter().GetResult();
            store.InvalidateCache();
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
            ulong scoreId = database.GetNextLeaderboardPvpScoreId() + 1ul;

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

            database.Save(context => context.LeaderboardPvpScore.Add(model)).GetAwaiter().GetResult();
            store.InvalidateCache();
        }

        private static string SerializeTeamMembers(IReadOnlyList<LeaderboardTeamMemberRecord> teamMembers)
        {
            if (teamMembers == null || teamMembers.Count == 0)
                return null;

            return JsonSerializer.Serialize(teamMembers);
        }
    }
}
