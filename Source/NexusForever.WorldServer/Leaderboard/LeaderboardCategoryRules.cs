using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    public static class LeaderboardCategoryRules
    {
        public static bool IsPveType(LeaderboardType type)
        {
            return type is LeaderboardType.PveDungeon
                or LeaderboardType.PveExpeditionGroup
                or LeaderboardType.PveExpeditionSolo;
        }

        public static bool IsPvpType(LeaderboardType type)
        {
            return !IsPveType(type);
        }

        public static bool MatchesPveScope(LeaderboardPveEntryRecord entry, LeaderboardType type, uint matchingGameMapId, uint primeLevel)
        {
            return entry.Type == type
                && entry.MatchingGameMapId == matchingGameMapId
                && entry.PrimeLevel == primeLevel;
        }

        public static bool MatchesPvpCategory(LeaderboardPvpEntryRecord entry, LeaderboardType type)
        {
            if (entry.Type != type)
                return false;

            return type switch
            {
                LeaderboardType.Arena3v3 => entry.Class == Class.PvpTeam,
                LeaderboardType.BattlegroundWarrior => entry.Class == Class.Warrior,
                LeaderboardType.BattlegroundEngineer => entry.Class == Class.Engineer,
                LeaderboardType.BattlegroundEsper => entry.Class == Class.Esper,
                LeaderboardType.BattlegroundMedic => entry.Class == Class.Medic,
                LeaderboardType.BattlegroundStalker => entry.Class == Class.Stalker,
                LeaderboardType.BattlegroundSpellslinger => entry.Class == Class.Spellslinger,
                _ => false
            };
        }

        public static Class GetBattlegroundClass(LeaderboardType type)
        {
            return type switch
            {
                LeaderboardType.BattlegroundWarrior => Class.Warrior,
                LeaderboardType.BattlegroundEngineer => Class.Engineer,
                LeaderboardType.BattlegroundEsper => Class.Esper,
                LeaderboardType.BattlegroundMedic => Class.Medic,
                LeaderboardType.BattlegroundStalker => Class.Stalker,
                LeaderboardType.BattlegroundSpellslinger => Class.Spellslinger,
                _ => Class.Warrior
            };
        }
    }
}
