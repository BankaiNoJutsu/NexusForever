using NexusForever.Database.Character;

namespace NexusForever.Game.Abstract.Entity
{
    public interface IGalacticArchiveManager : IDatabaseCharacter
    {
        bool UnlockArticle(uint archiveArticleId, bool grantRewards = true, bool unlockAllEntries = true);
        bool UnlockLinkedArticle(uint archiveArticleId);
        bool MarkArticleViewed(uint archiveArticleId);
        void RefreshRuleUnlocks();
        void SendInitialPackets();
    }
}
