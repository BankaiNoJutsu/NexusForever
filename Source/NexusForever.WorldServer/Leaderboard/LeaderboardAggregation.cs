using System;
using System.Collections.Generic;
using System.Linq;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Leaderboard;
using NexusForever.Network.World.Message.Model.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    public static class LeaderboardAggregation
    {
        public const int MaxVisibleRows = 50;

        public static ulong ComputeNextHourFileTimeUtc(DateTime utcNow)
        {
            DateTime nextHour = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
            return (ulong)nextHour.ToFileTimeUtc();
        }

        public static ServerLeaderboardPve BuildPveResponse(
            ClientLeaderboardPveRequest request,
            IReadOnlyList<LeaderboardPveEntryRecord> entries,
            IPlayer viewer,
            DateTime utcNow)
        {
            List<LeaderboardPveEntryRecord> fullRanked = RankPve(entries, request.Type, request.MatchingGameMapdId, request.PrimeLevel, viewer);
            List<LeaderboardPveEntryRecord> visible = TakeVisibleWithPersonalPlacement(fullRanked, viewer?.CharacterId ?? 0ul, MaxVisibleRows);
            Dictionary<ulong, uint> rankByCharacter = BuildRankIndex(fullRanked);

            return new ServerLeaderboardPve
            {
                Type              = request.Type,
                MatchingGameMapId = request.MatchingGameMapdId,
                PrimeLevel        = request.PrimeLevel,
                NextUpdateTime    = ComputeNextHourFileTimeUtc(utcNow),
                Players           = visible.Select(entry => ToWirePve(entry, rankByCharacter[entry.CharacterId])).ToList()
            };
        }

        public static ServerLeaderboardPvp BuildPvpResponse(
            ClientLeaderboardPvpRequest request,
            IReadOnlyList<LeaderboardPvpEntryRecord> entries,
            IPlayer viewer,
            DateTime utcNow)
        {
            List<LeaderboardPvpEntryRecord> fullRanked = RankPvp(entries, request.Type, viewer);
            List<LeaderboardPvpEntryRecord> visible = TakeVisibleWithPersonalPlacement(fullRanked, viewer?.CharacterId ?? 0ul, MaxVisibleRows);
            Dictionary<ulong, uint> rankByCharacter = BuildRankIndex(fullRanked);

            return new ServerLeaderboardPvp
            {
                Type           = request.Type,
                NextUpdateTime = ComputeNextHourFileTimeUtc(utcNow),
                Players        = visible.Select(entry => ToWirePvp(entry, rankByCharacter[entry.CharacterId])).ToList()
            };
        }

        public static List<LeaderboardPveEntryRecord> RankPve(
            IReadOnlyList<LeaderboardPveEntryRecord> entries,
            LeaderboardType type,
            uint matchingGameMapId,
            uint primeLevel,
            IPlayer viewer)
        {
            List<LeaderboardPveEntryRecord> scoped = entries
                .Where(entry => LeaderboardCategoryRules.MatchesPveScope(entry, type, matchingGameMapId, primeLevel))
                .OrderBy(entry => entry.CompletionTime)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .ToList();

            LeaderboardPveEntryRecord viewerEntry = TryCreateViewerPveEntry(viewer, type, matchingGameMapId, primeLevel, scoped);
            if (viewerEntry != null && scoped.All(entry => entry.CharacterId != viewerEntry.CharacterId))
                scoped.Add(viewerEntry);

            scoped = scoped
                .OrderBy(entry => entry.CompletionTime)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .ToList();

            return scoped;
        }

        public static List<LeaderboardPvpEntryRecord> RankPvp(
            IReadOnlyList<LeaderboardPvpEntryRecord> entries,
            LeaderboardType type,
            IPlayer viewer)
        {
            List<LeaderboardPvpEntryRecord> scoped = entries
                .Where(entry => LeaderboardCategoryRules.MatchesPvpCategory(entry, type))
                .OrderByDescending(entry => entry.Rating)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .ToList();

            LeaderboardPvpEntryRecord viewerEntry = TryCreateViewerPvpEntry(viewer, type, scoped);
            if (viewerEntry != null && scoped.All(entry => entry.CharacterId != viewerEntry.CharacterId))
                scoped.Add(viewerEntry);

            scoped = scoped
                .OrderByDescending(entry => entry.Rating)
                .ThenBy(entry => entry.Name, StringComparer.Ordinal)
                .ToList();

            return scoped;
        }

        private static Dictionary<ulong, uint> BuildRankIndex<T>(IReadOnlyList<T> ranked)
            where T : class
        {
            var rankByCharacter = new Dictionary<ulong, uint>(ranked.Count);
            for (int index = 0; index < ranked.Count; index++)
                rankByCharacter[GetCharacterId(ranked[index])] = (uint)(index + 1);

            return rankByCharacter;
        }

        private static List<T> TakeVisibleWithPersonalPlacement<T>(List<T> ranked, ulong viewerCharacterId, int maxVisible)
            where T : class
        {
            if (ranked.Count <= maxVisible)
                return ranked;

            if (viewerCharacterId == 0ul)
                return ranked.Take(maxVisible).ToList();

            int viewerIndex = ranked.FindIndex(entry => GetCharacterId(entry) == viewerCharacterId);
            if (viewerIndex < 0 || viewerIndex < maxVisible)
                return ranked.Take(maxVisible).ToList();

            List<T> visible = ranked.Take(maxVisible - 1).ToList();
            visible.Add(ranked[viewerIndex]);
            return visible;
        }

        private static ulong GetCharacterId<T>(T entry)
        {
            return entry switch
            {
                LeaderboardPveEntryRecord pve => pve.CharacterId,
                LeaderboardPvpEntryRecord pvp => pvp.CharacterId,
                _ => 0ul
            };
        }

        private static LeaderboardPveEntryRecord TryCreateViewerPveEntry(
            IPlayer viewer,
            LeaderboardType type,
            uint matchingGameMapId,
            uint primeLevel,
            IReadOnlyList<LeaderboardPveEntryRecord> ranked)
        {
            if (viewer == null || !LeaderboardCategoryRules.IsPveType(type))
                return null;

            if (ranked.Any(entry => entry.CharacterId == viewer.CharacterId))
                return null;

            uint completionTime = 900_000u + (uint)(viewer.CharacterId % 50_000u);
            return new LeaderboardPveEntryRecord
            {
                CharacterId       = viewer.CharacterId,
                Name              = viewer.Name,
                Class             = viewer.Class,
                GuildId           = viewer.GuildManager.GuildAffiliation?.Id ?? 0ul,
                Type              = type,
                MatchingGameMapId = matchingGameMapId,
                PrimeLevel        = primeLevel,
                RewardedTier      = 1u,
                CompletionTime    = completionTime,
                LastRank          = 0u,
                TeamMembers       = [new LeaderboardTeamMemberRecord { Name = viewer.Name, Class = viewer.Class }]
            };
        }

        private static LeaderboardPvpEntryRecord TryCreateViewerPvpEntry(
            IPlayer viewer,
            LeaderboardType type,
            IReadOnlyList<LeaderboardPvpEntryRecord> ranked)
        {
            if (viewer == null || !LeaderboardCategoryRules.IsPvpType(type))
                return null;

            if (ranked.Any(entry => entry.CharacterId == viewer.CharacterId))
                return null;

            Class rowClass = type == LeaderboardType.Arena3v3
                ? Class.PvpTeam
                : LeaderboardCategoryRules.GetBattlegroundClass(type);

            uint rating = 1200u + (uint)(viewer.CharacterId % 400u);
            return new LeaderboardPvpEntryRecord
            {
                CharacterId = viewer.CharacterId,
                Name        = viewer.Name,
                Class       = rowClass,
                GuildId     = viewer.GuildManager.GuildAffiliation?.Id ?? 0ul,
                Type        = type,
                Rating      = rating,
                LastRank    = 0u,
                TeamMembers = type == LeaderboardType.Arena3v3
                    ?
                    [
                        new LeaderboardTeamMemberRecord { Name = viewer.Name, Class = viewer.Class }
                    ]
                    : []
            };
        }

        private static LeaderboardPlayerPve ToWirePve(LeaderboardPveEntryRecord entry, uint rank)
        {
            return new LeaderboardPlayerPve
            {
                GuildId           = entry.GuildId,
                Class             = entry.Class,
                MatchingGameMapId = entry.MatchingGameMapId,
                PrimeLevel        = entry.PrimeLevel,
                RewardedTier      = entry.RewardedTier,
                CompletionTime    = entry.CompletionTime,
                Rank              = rank,
                LastRank          = entry.LastRank,
                Name              = entry.Name,
                TeamMembers       = entry.TeamMembers.Select(member => new TeamMember
                {
                    Name  = member.Name,
                    Class = member.Class
                }).ToList()
            };
        }

        private static LeaderboardTeamPvp ToWirePvp(LeaderboardPvpEntryRecord entry, uint rank)
        {
            return new LeaderboardTeamPvp
            {
                GuildId     = entry.GuildId,
                Class       = entry.Class,
                Rating      = entry.Rating,
                Rank        = rank,
                LastRank    = entry.LastRank,
                Name        = entry.Name,
                TeamMembers = entry.TeamMembers.Select(member => new TeamMember
                {
                    Name  = member.Name,
                    Class = member.Class
                }).ToList()
            };
        }
    }
}
