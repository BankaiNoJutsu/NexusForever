using NexusForever.Game.Abstract.Account.Reward;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Challenges
{
    public sealed partial class ChallengeManager
    {
        private sealed record ProducerAttempt(uint Track, CharacterChallengeRewardMode Mode);
        private sealed record ProducerTransition(RewardIntent Intent, string PreviousAttempt, bool Activation);
        private readonly Dictionary<string, ProducerAttempt> provisionalAttempts = new(StringComparer.Ordinal);
        private readonly List<ProducerTransition> producerTransitions = [];

        private bool SupportsDurableProducer(ChallengeEntry entry)
        {
            if (!characterRewardLoadRequired || rewardQueue == null || !CharacterRewardsReady || player.QuestManager is not QuestManager
                || (entry.ChallengeFlags & TimeTieredFlag) != 0
                || !characterRewardCatalog.TryGetTrack(entry.RewardTrackId, out _)) return false;
            // GetTierGoalCounts compacts its result. A missing source ordinal
            // cannot safely be reinterpreted as an earlier medal.
            bool ended = false;
            foreach (uint tier in new[] { entry.ChallengeTierId00, entry.ChallengeTierId01, entry.ChallengeTierId02 })
            {
                if (tier == 0) { ended = true; continue; }
                if (ended || GetTierCount(tier) == 0) return false;
            }
            return entry.ChallengeTierId00 != 0;
        }

        private bool CaptureProducerModifier(out ChallengeRewardModifierSnapshot modifier)
        {
            modifier = default;
            return player.Account?.RewardPropertyManager?.CaptureChallengeModifier(out modifier) == true
                && modifier.TryGetPoints(100, out _);
        }

        private ProducerAttempt ProducerMode(ChallengeRuntimeState state)
        {
            if (!characterRewardLoadRequired || state.ActiveAttemptId == null) return null; // historical/unsupported LegacyDirect stays NULL
            if (provisionalAttempts.TryGetValue(state.ActiveAttemptId, out var provisional)) return provisional;
            var durable = committedCharacterRewards?.Attempts.SingleOrDefault(row => row.AttemptId == state.ActiveAttemptId
                && row.ChallengeId == state.ChallengeId);
            if (durable == null) throw new InvalidOperationException("Live Challenge lost its durable reward mode.");
            return new(durable.TrackId, durable.Mode);
        }

        private bool QueueProducerActivation(ChallengeRuntimeState state, ChallengeEntry entry, string previousAttempt, CharacterChallengeState legacy)
        {
            uint[] prerequisites = CaptureRewardPrerequisites();
            var track = characterRewardCatalog.GetTrack(entry.RewardTrackId);
            bool enabled = (track.PrerequisiteId == 0 || prerequisites.Contains(track.PrerequisiteId)) && CaptureProducerModifier(out _);
            var mode = enabled ? CharacterChallengeRewardMode.CharacterTrack : CharacterChallengeRewardMode.LegacyDirect;
            provisionalAttempts.Add(state.ActiveAttemptId, new(entry.RewardTrackId, mode));
            var after = CharacterChallengeState.FromModel(BuildModel(state));
            if (CapturingContractCompanion)
            {
                var before = contractCompanion.Expected(after.ChallengeId);
                contractCompanion.Add(legacy == null ? CharacterChallengeRewardCommand.Activate(before, after, after.AttemptId, enabled, prerequisites)
                    : CharacterChallengeRewardCommand.ActivateFromLegacy(before, legacy, after, after.AttemptId, enabled, prerequisites));
                return true;
            }
            return EnqueueProducer(state, previousAttempt, true, held => legacy == null
                ? CharacterChallengeRewardCommand.Activate(held.Expected(after.ChallengeId), after, after.AttemptId, enabled, prerequisites)
                : CharacterChallengeRewardCommand.ActivateFromLegacy(held.Expected(after.ChallengeId), legacy, after, after.AttemptId, enabled, prerequisites));
        }

        private bool QueueProducerTransition(ChallengeRuntimeState state, byte? medal, bool completion,
            ChallengeRewardModifierSnapshot modifier = default)
        {
            Action afterCommit = medal.HasValue ? CaptureMedalNotices(state, medal.Value, completion) : null;
            if (CapturingContractCompanion) return CaptureContractTransition(state, medal, completion, modifier, afterCommit);
            var mode = ProducerMode(state);
            var after = CharacterChallengeState.FromModel(BuildModel(state));
            var created = DateTime.UtcNow;
            QuestManager.QuestRewardCapture capture = null;
            try
            {
                if (completion)
                {
                    if (player.QuestManager is not QuestManager quests || !quests.TryCaptureRewardCompletion(NormalSaveIdentity.ExecutionDatabase, () =>
                    {
                        if (mode.Mode == CharacterChallengeRewardMode.LegacyDirect)
                            GrantRewardTrackItem(gameTableManager.Challenge.GetEntry(state.ChallengeId), state.LastRewardTier);
                        player.QuestManager.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1);
                    }, out capture)) return RejectProducer();
                }
                bool queued = EnqueueProducer(state, after.AttemptId, false, held => medal.HasValue
                    ? mode.Mode == CharacterChallengeRewardMode.CharacterTrack
                        ? CharacterChallengeRewardCommand.Credit(held.Expected(after.ChallengeId), after, medal.Value, modifier, created)
                        : CharacterChallengeRewardCommand.AdvanceLegacy(held.Expected(after.ChallengeId), after, medal.Value)
                    : CharacterChallengeRewardCommand.Finish(held.Expected(after.ChallengeId), after), capture, afterCommit);
                if (!queued) capture?.Dispose();
                return queued;
            }
            catch { capture?.Dispose(); RejectProducer(); throw; }
        }

        private bool EnqueueProducer(ChallengeRuntimeState state, string previousAttempt, bool activation,
            Func<RewardPersistenceLease, CharacterChallengeRewardCommand> build, QuestManager.QuestRewardCapture quests = null,
            Action afterCommit = null)
        {
            var intent = CaptureRewardIntent((ushort)state.ChallengeId);
            var transition = new ProducerTransition(intent, previousAttempt, activation);
            producerTransitions.Add(transition);
            try
            {
                if (rewardQueue.Enqueue([intent], build, quests: quests, afterCommit: afterCommit)) return true;
            }
            catch { producerTransitions.Remove(transition); RejectProducer(); throw; }
            producerTransitions.Remove(transition); return RejectProducer();
        }

        private bool RejectProducer()
        {
            RequireRewardReload();
            if (player.QuestManager is QuestManager quests) quests.RequireNormalSaveReload();
            InvalidateRewardOperations();
            player.Session?.ForceDisconnect();
            return false;
        }

        // Old command adoption may encounter an immediately restarted attempt.
        // Only the exact queued producer lineage permits retaining that identity;
        // arbitrary external identity replacements still fail the normal merge.
        private bool RetainsLaterProducerIdentity(RewardIntent intent, CharacterChallengeState current)
        {
            int index = producerTransitions.FindIndex(row => ReferenceEquals(row.Intent, intent));
            if (index < 0 || current == null || intent.Runtime == null
                || current.CharacterId != intent.Runtime.CharacterId || current.ChallengeId != intent.ChallengeId
                || current.AttemptId == intent.Runtime.AttemptId) return false;
            string cursor = intent.Runtime.AttemptId;
            foreach (var next in producerTransitions.Skip(index + 1).Where(row => row.Intent.ChallengeId == intent.ChallengeId && row.Activation))
            {
                if (next.PreviousAttempt != cursor) return false;
                cursor = next.Intent.Runtime.AttemptId;
            }
            return cursor != intent.Runtime.AttemptId && cursor == current.AttemptId;
        }

        private void SettleProducerIntent(RewardIntent intent)
        {
            producerTransitions.RemoveAll(row => ReferenceEquals(row.Intent, intent));
            if (intent.Runtime?.AttemptId is string id && !producerTransitions.Any(row => row.Intent.Runtime?.AttemptId == id)
                && committedCharacterRewards?.Attempts.Any(row => row.AttemptId == id) == true)
                provisionalAttempts.Remove(id);
        }
    }
}
