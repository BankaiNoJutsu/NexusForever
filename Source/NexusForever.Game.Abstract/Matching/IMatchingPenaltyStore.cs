using MatchType = NexusForever.Game.Static.Matching.MatchType;

namespace NexusForever.Game.Abstract.Matching
{
    public readonly record struct MatchingPenaltyState(
        bool IsPvP,
        MatchType MatchType,
        uint Spell4Id,
        DateTimeOffset ExpiresAtUtc);

    public interface IMatchingPenaltyStore
    {
        MatchingPenaltyState? Get(ulong characterId);

        void Save(ulong characterId, MatchingPenaltyState state);

        void Delete(ulong characterId);
    }
}
