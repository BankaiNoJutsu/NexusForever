using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Challenges.Reward
{
    public enum CharacterChallengeRewardMode : byte { LegacyDirect, CharacterTrack }

    /// <summary>A copied actor/persistence boundary, never a tracked EF entity.</summary>
    public sealed record CharacterChallengeState(ulong CharacterId, ushort ChallengeId, string AttemptId,
        bool Activated, bool OnCooldown, bool LeftArea, uint Count, uint Tier, uint LastRewardTier,
        uint Completions, double ActiveSeconds, double CooldownSeconds, double AreaSeconds)
    {
        public static CharacterChallengeState FromModel(CharacterChallengeModel row) => row == null ? null : new(
            row.Id, row.ChallengeId, row.ActiveAttemptId, row.Activated, row.OnCooldown, row.LeftArea,
            row.CurrentCount, row.CurrentTier, row.LastRewardTier, row.CompletionCount,
            row.ActiveTimerSeconds, row.CooldownTimerSeconds, row.AreaFailTimerSeconds);

        public CharacterChallengeModel ToModel() => new()
        {
            Id = CharacterId, ChallengeId = ChallengeId, ActiveAttemptId = AttemptId,
            Activated = Activated, OnCooldown = OnCooldown, LeftArea = LeftArea,
            CurrentCount = Count, CurrentTier = Tier, LastRewardTier = LastRewardTier,
            CompletionCount = Completions, ActiveTimerSeconds = ActiveSeconds,
            CooldownTimerSeconds = CooldownSeconds, AreaFailTimerSeconds = AreaSeconds
        };
    }

    public sealed record CharacterChallengeSlotState(byte Ordinal, uint RewardRowId, uint RequiredPoints, bool Claimed);
    public sealed record CharacterChallengeTrackState(uint TrackId, ulong CycleId, uint Points, ulong Revision,
        bool Active, IReadOnlyList<CharacterChallengeSlotState> Slots);
    public sealed record CharacterChallengeAttemptState(string AttemptId, ushort ChallengeId, uint TrackId,
        CharacterChallengeRewardMode Mode, uint EarnedMedalMask, bool Terminal);
    public sealed record CharacterChallengeMedalState(bool Applied, byte Medal, uint Points, float Multiplier,
        long ModifierRevision, CharacterChallengeAttemptState Attempt, CharacterChallengeTrackState Track);
    public sealed record CharacterChallengeClaimState(bool Applied, string ClaimId, uint TrackId, ulong CycleId,
        byte Ordinal, uint RewardRowId, uint Choice, CharacterChallengeTrackState Track);
    public sealed record CharacterChallengeRewardsState(IReadOnlyList<CharacterChallengeTrackState> Tracks,
        IReadOnlyList<CharacterChallengeAttemptState> Attempts)
    {
        public IReadOnlyList<CharacterChallengeState> Challenges { get; init; } = Array.Empty<CharacterChallengeState>();
        internal IReadOnlyList<Quest.Reward.ContractTrackState> ContractTracks { get; init; }
    }
}
