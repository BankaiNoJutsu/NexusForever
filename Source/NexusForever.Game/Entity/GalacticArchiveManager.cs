using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.GalacticArchive;

namespace NexusForever.Game.Entity
{
    public class GalacticArchiveManager : IGalacticArchiveManager
    {
        private const uint ArticleUnlockedFlag = 0x80000000u;
        private const string ArchiveEntryTableName = "ArchiveEntry.tbl";

        private readonly IPlayer player;
        private readonly IGameTableManager gameTableManager;
        private readonly Dictionary<uint, ArchiveArticleState> articles = new();

        public GalacticArchiveManager(
            IPlayer player,
            CharacterModel model,
            IGameTableManager gameTableManager = null)
        {
            this.player = player;
            this.gameTableManager = gameTableManager;

            GameTable<ArchiveArticleEntry> archiveArticleTable = gameTableManager.ArchiveArticle;
            foreach (CharacterGalacticArchiveModel archiveModel in model.GalacticArchive)
            {
                if (archiveArticleTable?.GetEntry(archiveModel.ArchiveArticleId) == null)
                    continue;

                articles[archiveModel.ArchiveArticleId] = ArchiveArticleState.FromModel(archiveModel);
            }
        }

        public void Save(CharacterContext context)
        {
            foreach (ArchiveArticleState article in articles.Values)
            {
                CharacterGalacticArchiveModel model = article.BuildModel();
                if (article.PendingCreate)
                    context.Add(model);
                else if (article.Dirty)
                {
                    EntityEntry<CharacterGalacticArchiveModel> entity = context.Attach(model);
                    entity.Property(p => p.UnlockedFlags).IsModified = true;
                    entity.Property(p => p.ViewedFlags).IsModified = true;
                }

                article.ClearSaveState();
            }
        }

        public bool UnlockArticle(uint archiveArticleId, bool grantRewards = true, bool unlockAllEntries = true)
        {
            ArchiveArticleEntry articleEntry = gameTableManager.ArchiveArticle?.GetEntry(archiveArticleId);
            if (articleEntry == null)
                return false;

            ArchiveArticleState article = GetOrCreateArticle(archiveArticleId);
            uint previousFlags = article.UnlockedFlags;
            uint unlockedFlags = ArticleUnlockedFlag;
            if (unlockAllEntries)
                unlockedFlags |= GetAllEntryFlags(articleEntry);

            article.UnlockedFlags |= unlockedFlags;
            if (article.UnlockedFlags == previousFlags)
                return true;

            article.MarkDirty();
            SendUpdate(article);

            if (grantRewards)
                GrantUnlockRewards(articleEntry, previousFlags, article.UnlockedFlags);

            return true;
        }

        public bool UnlockLinkedArticle(uint archiveArticleId)
        {
            if (gameTableManager.ArchiveArticle?.GetEntry(archiveArticleId) == null)
                return false;

            if (IsArticleUnlocked(archiveArticleId))
                return true;

            GameTable<ArchiveLinkEntry> archiveLinkTable = gameTableManager.ArchiveLink;
            if (archiveLinkTable == null)
                return false;

            bool hasUnlockedParent = archiveLinkTable.Entries.Any(link =>
                link.ArchiveArticleIdChild == archiveArticleId
                && link.ArchiveArticleIdParent != 0u
                && IsArticleUnlocked(link.ArchiveArticleIdParent));
            if (!hasUnlockedParent)
                return false;

            return UnlockArticle(archiveArticleId, grantRewards: false);
        }

        public bool MarkArticleViewed(uint archiveArticleId)
        {
            ArchiveArticleEntry articleEntry = gameTableManager.ArchiveArticle?.GetEntry(archiveArticleId);
            if (articleEntry == null)
                return false;

            if (!articles.TryGetValue(archiveArticleId, out ArchiveArticleState article))
                return false;

            uint previousFlags = article.ViewedFlags;
            article.ViewedFlags |= article.UnlockedFlags & GetAllEntryFlags(articleEntry);
            if (article.ViewedFlags == previousFlags)
                return true;

            article.MarkDirty();
            SendUpdate(article);
            return true;
        }

        public void RefreshRuleUnlocks()
        {
            GameTable<ArchiveArticleEntry> archiveArticleTable = gameTableManager.ArchiveArticle;
            if (archiveArticleTable == null)
                return;

            foreach (ArchiveArticleEntry articleEntry in archiveArticleTable.Entries)
            {
                uint ruleFlags = GetSatisfiedRuleEntryFlags(articleEntry);
                if (ruleFlags == 0u)
                    continue;

                ArchiveArticleState article = GetOrCreateArticle(articleEntry.Id);
                uint previousFlags = article.UnlockedFlags;
                article.UnlockedFlags |= ArticleUnlockedFlag | ruleFlags;
                if (article.UnlockedFlags == previousFlags)
                    continue;

                article.MarkDirty();
                GrantUnlockRewards(articleEntry, previousFlags, article.UnlockedFlags);
            }
        }

        public void SendInitialPackets()
        {
            RefreshRuleUnlocks();

            player.Session.EnqueueMessageEncrypted(new ServerGalacticArchiveRefresh());
            foreach (ArchiveArticleState article in articles.Values.OrderBy(a => a.ArchiveArticleId))
                SendUpdate(article);
        }

        private ArchiveArticleState GetOrCreateArticle(uint archiveArticleId)
        {
            if (articles.TryGetValue(archiveArticleId, out ArchiveArticleState article))
                return article;

            article = ArchiveArticleState.Create(player.CharacterId, archiveArticleId);
            articles.Add(archiveArticleId, article);
            return article;
        }

        private bool IsArticleUnlocked(uint archiveArticleId)
        {
            return articles.TryGetValue(archiveArticleId, out ArchiveArticleState article)
                && (article.UnlockedFlags & ArticleUnlockedFlag) != 0u;
        }

        private uint GetSatisfiedRuleEntryFlags(ArchiveArticleEntry articleEntry)
        {
            GameTable<ArchiveEntryUnlockRuleEntry> ruleTable = gameTableManager.ArchiveEntryUnlockRule;
            if (ruleTable == null)
                return 0u;

            uint flags = 0u;
            IReadOnlyList<uint> entryIds = GetEntryIds(articleEntry);
            for (int i = 0; i < entryIds.Count; i++)
            {
                uint entryId = entryIds[i];
                if (entryId == 0u)
                    continue;

                List<ArchiveEntryUnlockRuleEntry> rules = ruleTable.Entries
                    .Where(rule => rule.ArchiveEntryId == entryId)
                    .ToList();
                if (rules.Count == 0)
                    continue;

                if (rules.Any(IsRuleSatisfied))
                    flags |= 1u << i;
            }

            return flags;
        }

        private bool IsRuleSatisfied(ArchiveEntryUnlockRuleEntry rule)
        {
            return GalacticArchiveUnlockRule.IsSatisfied(
                rule,
                IsAchievementComplete,
                IsQuestComplete,
                player.PathManager.IsMissionComplete);
        }

        private bool IsAchievementComplete(uint objectId)
        {
            return objectId <= ushort.MaxValue
                && player.AchievementManager.HasCompletedAchievement((ushort)objectId);
        }

        private bool IsQuestComplete(uint objectId)
        {
            if (objectId == 0u)
                return false;

            return objectId <= ushort.MaxValue
                && player.QuestManager.GetQuestState((ushort)objectId) == QuestState.Completed;
        }

        private void GrantUnlockRewards(ArchiveArticleEntry articleEntry, uint previousFlags, uint currentFlags)
        {
            GameTable<ArchiveEntryEntry> archiveEntryTable = gameTableManager.ArchiveEntry;
            if (archiveEntryTable == null)
            {
                MissingGameDataDiagnostics.ReportMissingTable(
                    ArchiveEntryTableName,
                    nameof(GalacticArchiveManager) + "." + nameof(GrantUnlockRewards),
                    MissingGameDataSeverity.PlayerImpacting,
                    $"Cannot grant archive entry rewards for archiveArticleId={articleEntry.Id}.");
            }

            if ((previousFlags & ArticleUnlockedFlag) == 0u
                && (currentFlags & ArticleUnlockedFlag) != 0u
                && articleEntry.CharacterTitleIdReward != 0u
                && articleEntry.CharacterTitleIdReward <= ushort.MaxValue)
                player.TitleManager.AddTitle((ushort)articleEntry.CharacterTitleIdReward);

            IReadOnlyList<uint> entryIds = GetEntryIds(articleEntry);
            for (int i = 0; i < entryIds.Count; i++)
            {
                uint flag = 1u << i;
                if ((previousFlags & flag) != 0u || (currentFlags & flag) == 0u)
                    continue;

                if (archiveEntryTable == null)
                    continue;

                ArchiveEntryEntry entry = archiveEntryTable.GetEntry(entryIds[i]);
                if (entry == null)
                {
                    MissingGameDataDiagnostics.ReportMissingRow(
                        ArchiveEntryTableName,
                        entryIds[i],
                        nameof(GalacticArchiveManager) + "." + nameof(GrantUnlockRewards),
                        MissingGameDataSeverity.PlayerImpacting,
                        $"Cannot grant archive entry reward for archiveArticleId={articleEntry.Id}.");
                    continue;
                }

                if (entry?.CharacterTitleIdReward > 0u && entry.CharacterTitleIdReward <= ushort.MaxValue)
                    player.TitleManager.AddTitle((ushort)entry.CharacterTitleIdReward);
            }
        }

        private static uint GetAllEntryFlags(ArchiveArticleEntry articleEntry)
        {
            uint flags = 0u;
            IReadOnlyList<uint> entryIds = GetEntryIds(articleEntry);
            for (int i = 0; i < entryIds.Count; i++)
                if (entryIds[i] != 0u)
                    flags |= 1u << i;

            return flags;
        }

        private static IReadOnlyList<uint> GetEntryIds(ArchiveArticleEntry entry)
        {
            return
            [
                entry.ArchiveEntryId00,
                entry.ArchiveEntryId01,
                entry.ArchiveEntryId02,
                entry.ArchiveEntryId03,
                entry.ArchiveEntryId04,
                entry.ArchiveEntryId05,
                entry.ArchiveEntryId06,
                entry.ArchiveEntryId07,
                entry.ArchiveEntryId08,
                entry.ArchiveEntryId09,
                entry.ArchiveEntryId10,
                entry.ArchiveEntryId11,
                entry.ArchiveEntryId12,
                entry.ArchiveEntryId13,
                entry.ArchiveEntryId14,
                entry.ArchiveEntryId15
            ];
        }

        private void SendUpdate(ArchiveArticleState article)
        {
            player.Session.EnqueueMessageEncrypted(new ServerGalacticArchiveUpdate
            {
                ArchiveArticleId = article.ArchiveArticleId,
                UnlockedFlags    = article.UnlockedFlags,
                ViewedFlags      = article.ViewedFlags
            });
        }

        private sealed class ArchiveArticleState
        {
            public ulong CharacterId { get; init; }
            public uint ArchiveArticleId { get; init; }
            public uint UnlockedFlags { get; set; }
            public uint ViewedFlags { get; set; }
            public bool PendingCreate { get; private set; }
            public bool Dirty { get; private set; }

            public static ArchiveArticleState Create(ulong characterId, uint archiveArticleId)
            {
                return new ArchiveArticleState
                {
                    CharacterId      = characterId,
                    ArchiveArticleId = archiveArticleId,
                    PendingCreate    = true
                };
            }

            public static ArchiveArticleState FromModel(CharacterGalacticArchiveModel model)
            {
                return new ArchiveArticleState
                {
                    CharacterId      = model.Id,
                    ArchiveArticleId = model.ArchiveArticleId,
                    UnlockedFlags    = model.UnlockedFlags,
                    ViewedFlags      = model.ViewedFlags
                };
            }

            public void MarkDirty()
            {
                if (!PendingCreate)
                    Dirty = true;
            }

            public void ClearSaveState()
            {
                PendingCreate = false;
                Dirty         = false;
            }

            public CharacterGalacticArchiveModel BuildModel()
            {
                return new CharacterGalacticArchiveModel
                {
                    Id               = CharacterId,
                    ArchiveArticleId = ArchiveArticleId,
                    UnlockedFlags    = UnlockedFlags,
                    ViewedFlags      = ViewedFlags
                };
            }
        }
    }
}
