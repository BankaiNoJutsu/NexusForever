using NexusForever.Database.Auth;
using NexusForever.Game.Challenges;

namespace NexusForever.Game.Entity
{
    public partial class Player
    {
        private NormalRewardSave normalRewardSave;

        // The queue has already discovered this identity on its worker. Capturing
        // normal-save targets here performs no provider initialization or SQL.
        internal NormalRewardSave CaptureNormalRewardSave(ChallengeManager.RewardPersistenceIdentity identity = null)
        {
            if (normalRewardSave != null) throw new InvalidOperationException("Player already has a frozen normal save.");
            if (QuestManager is not QuestManager quests || ChallengeManager is not ChallengeManager challenges) return null;
            identity ??= challenges.NormalSaveIdentity;
            if (identity == null) return null; // existing startup/legacy path; reward capture still refuses overlap
            var questSnapshot = quests.CaptureNormalSave(identity.ExecutionDatabase);
            try
            {
                var currencySnapshot = (CurrencyManager as CurrencyManager)?.CaptureNormalSave(questSnapshot.Token);
                var challengeSnapshot = challenges.CaptureNormalSave(identity);
                normalRewardSave = new(this, quests, challenges, questSnapshot, challengeSnapshot, currencySnapshot); return normalRewardSave;
            }
            catch { questSnapshot.Dispose(); throw; }
        }

        internal sealed class NormalRewardSave(Player owner, QuestManager quests, ChallengeManager challenges,
            QuestManager.NormalSaveSnapshot questSnapshot, ChallengeManager.NormalSaveSnapshot challengeSnapshot,
            Action<NexusForever.Database.Character.CharacterContext> currencySnapshot)
        {
            internal QuestManager.NormalSaveSnapshot Quests { get; } = questSnapshot;
            internal ChallengeManager.NormalSaveSnapshot Challenges { get; } = challengeSnapshot;
            internal void SaveCurrencies(NexusForever.Database.Character.CharacterContext context)
            { if (currencySnapshot == null) owner.CurrencyManager.Save(context); else currencySnapshot(context); }
            private bool ended;
            internal void Validate()
            {
                if (ended || !ReferenceEquals(owner.normalRewardSave, this)) throw new InvalidOperationException("Player frozen save lost its owner.");
            }
            internal bool Finish(bool success)
            {
                if (ended) return false; Validate(); ended = true;
                success = Quests.FinishContract(success);
                success = Challenges.FinishContract(success);
                if (!success) Quarantine();
                Quests.Dispose(); Challenges.Dispose(); owner.normalRewardSave = null;
                bool accepted = success && !quests.RequiresAuthoritativeReload && !challenges.RequiresAuthoritativeReload;
                if (!accepted) Quarantine();
                else if (Quests.HasContractCompletion)
                {
                    try { quests.PublishContractRewards(); }
                    catch { Quarantine(); return false; }
                }
                return accepted;
            }
            private void Quarantine()
            {
                quests.RequireNormalSaveReload(); challenges.RequireRewardReload();
                quests.InvalidateRewardOperations(true); challenges.InvalidateRewardOperations(true);
            }
        }

        private void SaveFrozenAuth(AuthContext context, NormalRewardSave snapshot)
        {
            if (snapshot == null) { Save(context); return; }
            snapshot.Validate(); CheckInventorySaveAllowed(frozen: true); Account.Save(context);
        }
        private bool ReleaseNormalRewardSave(bool success) => normalRewardSave?.Finish(success) ?? success;
    }
}
