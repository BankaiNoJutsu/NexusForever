using System;
using System.Collections.Generic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Network.World.Message.Model.Leaderboard;

namespace NexusForever.WorldServer.Leaderboard
{
    public sealed class LeaderboardProvider : ILeaderboardProvider
    {
        private readonly ILeaderboardStore store;
        private readonly Func<DateTime> utcNow;

        public LeaderboardProvider(ILeaderboardStore store, Func<DateTime> utcNow = null)
        {
            this.store  = store;
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public ServerLeaderboardPve BuildPve(ClientLeaderboardPveRequest request, IPlayer viewer = null)
        {
            IReadOnlyList<LeaderboardPveEntryRecord> entries = store.GetPveEntries(
                request.Type,
                request.MatchingGameMapdId,
                request.PrimeLevel);

            return LeaderboardAggregation.BuildPveResponse(request, entries, viewer, utcNow());
        }

        public ServerLeaderboardPvp BuildPvp(ClientLeaderboardPvpRequest request, IPlayer viewer = null)
        {
            IReadOnlyList<LeaderboardPvpEntryRecord> entries = store.GetPvpEntries(request.Type);

            return LeaderboardAggregation.BuildPvpResponse(request, entries, viewer, utcNow());
        }
    }
}
