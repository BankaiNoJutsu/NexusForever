namespace NexusForever.Game.Abstract.Matching
{
    public interface IMatchingDeserterManager
    {
        bool CanQueue(ulong characterId, Static.Matching.MatchType matchType);

        void ApplyDeserter(ulong characterId, Static.Matching.MatchType matchType, double completionRatio);

        void ClearDeserter(ulong characterId);

        void RestoreDeserter(NexusForever.Game.Abstract.Entity.IPlayer player);

        int GetRemainingPenaltyMilliseconds(ulong characterId);

        uint[] GetMatchingPenaltyTimesMilliseconds(ulong characterId);

        void SyncDeserterUi(NexusForever.Game.Abstract.Entity.IPlayer player);
    }
}
