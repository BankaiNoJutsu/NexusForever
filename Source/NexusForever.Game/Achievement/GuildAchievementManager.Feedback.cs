using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Guild;

namespace NexusForever.Game.Achievement
{
    public sealed partial class GuildAchievementManager
    {
        private sealed record Feedback(GuildAchievementFeedbackReceipt.Stored Receipt, CharacterAchievementManager Recipient);
        private readonly List<Feedback> feedback = [];
        private readonly Dictionary<ushort, (FeedbackWave Wave, int Order, Action Retry)> unfinishedFeedback = [];
        private readonly HashSet<FeedbackWave> unfinishedWaves = [];
        private sealed class FeedbackWave(CharacterAchievementManager recipient)
        {
            internal readonly CharacterAchievementManager Recipient = recipient;
            internal readonly List<(ushort Parent, string Plan)> Parts = [];
            internal int PreparationOrder;
        }
        private FeedbackWave currentFeedbackWave;
        internal bool HasPendingFeedback { get { lock (RowMutationGate ?? this) return feedback.Count != 0 || unfinishedWaves.Count != 0; } }
        private void CompleteWithFeedback(IPlayer actor, CharacterAchievementManager recipient, IAchievement parent, Action complete)
        {
            // Preparation can call eligibility providers. If one throws, this
            // transition must never later save an unaccompanied completion.
            FeedbackWave wave = currentFeedbackWave ?? new(recipient);
            unfinishedWaves.Add(wave);
            int order = ++wave.PreparationOrder;
            bool completed = false;
            IAchievementInfo[] forwarding = null;
            int forwardingCursor = 0;
            void Finish()
            {
                FeedbackWave previous = currentFeedbackWave; currentFeedbackWave = wave;
                try
                {
                    if (!ReferenceEquals(wave.Recipient, recipient)) throw new InvalidOperationException("Guild completion chain changed its accepted Character recipient.");
                    if (!completed)
                    {
                        string plan = recipient.PrepareGuildFeedback(parent.Id);
                        complete();
                        if (plan != null) wave.Parts.Add((parent.Id, plan));
                        completed = true;
                    }
                    // Keep Guild metas in this graph. Their ordered Character
                    // effects join one durable delivery after the whole chain.
                    forwarding ??= GetGlobalAchievementManager().GetGuildAchievements(AchievementType.AchievementComplete).ToArray();
                    while (forwardingCursor < forwarding.Length)
                    {
                        CheckAchievements(actor, [forwarding[forwardingCursor]], parent.Id, 0, 1);
                        forwardingCursor++;
                    }
                    unfinishedFeedback.Remove(parent.Id);
                    if (previous == null) FinishFeedbackWave(wave);
                }
                finally { currentFeedbackWave = previous; }
            }
            unfinishedFeedback[parent.Id] = (wave, order, Finish);
            Finish();
        }
        private void RetryUnfinishedFeedback(CharacterAchievementManager recipient)
        {
            // An inner row may already have complete progress but no accepted
            // feedback. Resume it before its parent's loop can skip that row.
            foreach (var pending in unfinishedFeedback.Values.Where(value => ReferenceEquals(value.Wave.Recipient, recipient))
                .OrderByDescending(value => value.Order).ToArray()) pending.Retry();
            foreach (FeedbackWave wave in unfinishedWaves.Where(wave => ReferenceEquals(wave.Recipient, recipient)).ToArray()) FinishFeedbackWave(wave);
        }
        private void FinishFeedbackWave(FeedbackWave wave)
        {
            if (!unfinishedWaves.Contains(wave) || unfinishedFeedback.Values.Any(pending => ReferenceEquals(pending.Wave, wave))) return;
            if (wave.Parts.Count != 0)
                feedback.Add(new(wave.Recipient.CreateGuildFeedback(guild.Id, wave.Parts.ToArray()), wave.Recipient));
            unfinishedWaves.Remove(wave);
        }
        internal void StageFeedback(CharacterContext context, GuildMutationPersistence.Achievement[] achievements)
        {
            _ = OriginalGuild();
            if (unfinishedWaves.Count != 0)
                throw new InvalidOperationException("Guild completion feedback preparation did not finish; its parent cannot commit.");
            ushort[] completed = achievements.Where(row => row.Completed != null).Select(row => row.Id).ToArray();
            foreach (Feedback pending in feedback.ToArray())
                GuildAchievementFeedbackReceipt.StageIntent(context, pending.Receipt, guild.Id, completed, () =>
                {
                    lock (RowMutationGate ?? this) feedback.Remove(pending);
                    pending.Recipient.EnqueueGuildFeedback(pending.Receipt);
                });
        }
    }
}
