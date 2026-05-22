using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    /// <summary>
    /// Deterministic in-memory leaderboard source until PvE/PvP rating persistence is mapped.
    /// </summary>
    public sealed class InMemoryLeaderboardStore : ILeaderboardStore
    {
        private readonly IReadOnlyList<LeaderboardPveEntryRecord> pveEntries;
        private readonly IReadOnlyList<LeaderboardPvpEntryRecord> pvpEntries;

        public InMemoryLeaderboardStore()
            : this(CreateDefaultPveEntries(), CreateDefaultPvpEntries())
        {
        }

        public InMemoryLeaderboardStore(
            IReadOnlyList<LeaderboardPveEntryRecord> pveEntries,
            IReadOnlyList<LeaderboardPvpEntryRecord> pvpEntries)
        {
            this.pveEntries = pveEntries;
            this.pvpEntries = pvpEntries;
        }

        public IReadOnlyList<LeaderboardPveEntryRecord> GetPveEntries(LeaderboardType type, uint matchingGameMapId, uint primeLevel)
        {
            return pveEntries
                .Where(entry => LeaderboardCategoryRules.MatchesPveScope(entry, type, matchingGameMapId, primeLevel))
                .ToList();
        }

        public IReadOnlyList<LeaderboardPvpEntryRecord> GetPvpEntries(LeaderboardType type)
        {
            return pvpEntries
                .Where(entry => LeaderboardCategoryRules.MatchesPvpCategory(entry, type))
                .ToList();
        }

        private static IReadOnlyList<LeaderboardPveEntryRecord> CreateDefaultPveEntries()
        {
            return
            [
                CreatePve(1001ul, "Archive Gold", Class.Esper, LeaderboardType.PveDungeon, 1234u, 5u, 412_000u, 4u, 3u,
                [
                    new LeaderboardTeamMemberRecord { Name = "Tank", Class = Class.Warrior },
                    new LeaderboardTeamMemberRecord { Name = "Healer", Class = Class.Medic }
                ]),
                CreatePve(1002ul, "Prime Runner", Class.Warrior, LeaderboardType.PveDungeon, 1234u, 5u, 455_500u, 2u, 2u,
                [
                    new LeaderboardTeamMemberRecord { Name = "Prime Runner", Class = Class.Warrior }
                ]),
                CreatePve(1003ul, "Expedition Lead", Class.Medic, LeaderboardType.PveExpeditionGroup, 1234u, 5u, 612_000u, 1u, 1u,
                [
                    new LeaderboardTeamMemberRecord { Name = "Expedition Lead", Class = Class.Medic },
                    new LeaderboardTeamMemberRecord { Name = "Support", Class = Class.Engineer }
                ]),
                CreatePve(1004ul, "Solo Scout", Class.Stalker, LeaderboardType.PveExpeditionSolo, 1234u, 5u, 388_250u, 3u, 5u, []),
                CreatePve(1005ul, "Dungeon Duo", Class.Spellslinger, LeaderboardType.PveDungeon, 77u, 1u, 501_000u, 5u, 6u,
                [
                    new LeaderboardTeamMemberRecord { Name = "Dungeon Duo", Class = Class.Spellslinger }
                ])
            ];
        }

        private static IReadOnlyList<LeaderboardPvpEntryRecord> CreateDefaultPvpEntries()
        {
            return
            [
                CreateArenaTeam(2001ul, "Rated Team", 2100u, 9u,
                [
                    new LeaderboardTeamMemberRecord { Name = "Lead", Class = Class.Spellslinger },
                    new LeaderboardTeamMemberRecord { Name = "Bruiser", Class = Class.Warrior },
                    new LeaderboardTeamMemberRecord { Name = "Support", Class = Class.Medic }
                ]),
                CreateArenaTeam(2002ul, "Top Shelf", 1985u, 3u,
                [
                    new LeaderboardTeamMemberRecord { Name = "Captain", Class = Class.Engineer }
                ]),
                CreateBattleground(2003ul, "BG Warrior", Class.Warrior, LeaderboardType.BattlegroundWarrior, 1850u, 4u),
                CreateBattleground(2004ul, "BG Medic", Class.Medic, LeaderboardType.BattlegroundMedic, 1765u, 2u),
                CreateBattleground(2005ul, "BG Esper", Class.Esper, LeaderboardType.BattlegroundEsper, 1690u, 6u),
                CreateBattleground(2006ul, "BG Stalker", Class.Stalker, LeaderboardType.BattlegroundStalker, 1625u, 8u),
                CreateBattleground(2007ul, "BG Engineer", Class.Engineer, LeaderboardType.BattlegroundEngineer, 1580u, 5u),
                CreateBattleground(2008ul, "BG Spellslinger", Class.Spellslinger, LeaderboardType.BattlegroundSpellslinger, 1510u, 7u)
            ];
        }

        private static LeaderboardPveEntryRecord CreatePve(
            ulong characterId,
            string name,
            Class playerClass,
            LeaderboardType type,
            uint matchingGameMapId,
            uint primeLevel,
            uint completionTime,
            uint rewardedTier,
            uint lastRank,
            IReadOnlyList<LeaderboardTeamMemberRecord> teamMembers)
        {
            return new LeaderboardPveEntryRecord
            {
                CharacterId       = characterId,
                Name              = name,
                Class             = playerClass,
                GuildId           = 0ul,
                Type              = type,
                MatchingGameMapId = matchingGameMapId,
                PrimeLevel        = primeLevel,
                RewardedTier      = rewardedTier,
                CompletionTime    = completionTime,
                LastRank          = lastRank,
                TeamMembers       = teamMembers
            };
        }

        private static LeaderboardPvpEntryRecord CreateArenaTeam(
            ulong characterId,
            string name,
            uint rating,
            uint lastRank,
            IReadOnlyList<LeaderboardTeamMemberRecord> teamMembers)
        {
            return new LeaderboardPvpEntryRecord
            {
                CharacterId = characterId,
                Name        = name,
                Class       = Class.PvpTeam,
                GuildId     = 0ul,
                Type        = LeaderboardType.Arena3v3,
                Rating      = rating,
                LastRank    = lastRank,
                TeamMembers = teamMembers
            };
        }

        private static LeaderboardPvpEntryRecord CreateBattleground(
            ulong characterId,
            string name,
            Class playerClass,
            LeaderboardType type,
            uint rating,
            uint lastRank)
        {
            return new LeaderboardPvpEntryRecord
            {
                CharacterId = characterId,
                Name        = name,
                Class       = playerClass,
                GuildId     = 0ul,
                Type        = type,
                Rating      = rating,
                LastRank    = lastRank,
                TeamMembers = []
            };
        }
    }
}
