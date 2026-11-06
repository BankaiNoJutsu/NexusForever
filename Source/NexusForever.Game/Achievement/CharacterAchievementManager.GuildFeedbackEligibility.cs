namespace NexusForever.Game.Achievement
{
    public sealed partial class CharacterAchievementManager
    {
        // Only causal achievement completion remains variable. All other gates
        // are evaluated on the accepting actor and never on a replacement Player.
        private sealed record FeedbackEligibility(ushort? Achievement, bool Accepted,
            FeedbackEligibility Incomplete = null, FeedbackEligibility Complete = null)
        {
            internal bool CanAccept => Achievement.HasValue ? Incomplete.CanAccept || Complete.CanAccept : Accepted;
        }
        private HashSet<ushort> feedbackVariableAchievements;
        private Dictionary<ushort, bool> feedbackEligibilityAssignments;
        private ushort? feedbackUnassignedAchievement;

        public override bool HasCompletedAchievement(ushort id)
        {
            bool completed = base.HasCompletedAchievement(id);
            if (completed || feedbackEligibilityAssignments == null || !feedbackVariableAchievements.Contains(id)) return completed;
            if (feedbackEligibilityAssignments.TryGetValue(id, out bool assigned)) return assigned;
            feedbackUnassignedAchievement ??= id;
            return false;
        }
        private static bool MatchesFeedbackObject(uint expected, uint alternate, ushort cause)
            => (expected == 0 || expected == cause) && alternate == 0;

        private FeedbackEligibility CaptureFeedbackEligibility(Func<bool> evaluate)
        {
            int evaluations = 0;
            FeedbackEligibility Capture()
            {
                // Refuse pathological provider expressions before accepting the
                // parent. Ordinary retail prerequisites have a small fixed graph.
                if (++evaluations > 1024 || feedbackEligibilityAssignments.Count > 24)
                    throw new InvalidOperationException("Guild feedback eligibility exceeds its bounded decision graph.");
                feedbackUnassignedAchievement = null;
                bool accepted = evaluate();
                if (feedbackUnassignedAchievement is not ushort id) return new(null, accepted);
                feedbackEligibilityAssignments.Add(id, false);
                FeedbackEligibility incomplete = Capture();
                feedbackEligibilityAssignments[id] = true;
                FeedbackEligibility complete = Capture();
                feedbackEligibilityAssignments.Remove(id);
                return incomplete == complete ? incomplete : new(id, false, incomplete, complete);
            }
            return Capture();
        }
        private bool EvaluateFeedbackEligibility(FeedbackEligibility decision)
        {
            while (decision.Achievement is ushort id)
                decision = activeFeedbackCompletions.Contains(id) ? decision.Complete : decision.Incomplete;
            return decision.Accepted;
        }
    }
}
