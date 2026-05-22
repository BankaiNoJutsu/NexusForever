namespace NexusForever.Game.Abstract.Matching
{
    public interface IMatchingDeserterManager
    {
        bool CanQueue(ulong characterId, Static.Matching.MatchType matchType);

        void ApplyDeserter(ulong characterId, Static.Matching.MatchType matchType, double completionRatio);

        void ClearDeserter(ulong characterId);

        int GetRemainingPenaltyMilliseconds(ulong characterId);

        void SyncDeserterUi(NexusForever.Game.Abstract.Entity.IPlayer player);
    }
}
