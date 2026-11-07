using NexusForever.Game.Static.Challenges;

namespace NexusForever.Game.Abstract.Challenges
{
    public interface IChallengeManager
    {
        bool HasPendingRewardOperation => false;
        bool RequiresAuthoritativeReload => false;
        void InvalidateRewardOperations(bool final = false) { }
        bool ChooseRewardTrack(uint trackId) => false;
        bool ClaimRewardTrack(uint rewardRowId, uint choice) => false;

        void SendInitialPackets();

        void Update(double lastTick);

        void HandleChoice(ushort challengeId, ChallengeChoice choice);

        /// <summary>
        /// Share an active challenge with the player's current target, mirroring quest-share targeting.
        /// </summary>
        void ShareWithTarget(ushort challengeId);

        /// <summary>
        /// Receive a shared-challenge invitation from another player.
        /// </summary>
        void ReceiveShare(ushort challengeId, uint sharerUnitId);

        /// <summary>
        /// Advance progress for an active challenge by challenge id.
        /// </summary>
        bool TryAdvanceProgress(ushort challengeId, uint progress = 1u);

        /// <summary>
        /// Returns the number of recorded completions for the supplied challenge.
        /// </summary>
        uint GetCompletionCount(ushort challengeId);

        /// <summary>
        /// Returns the current progress count of the supplied challenge, or 0 when the character has no record of it.
        /// </summary>
        uint GetCurrentCount(ushort challengeId);

        /// <summary>
        /// Returns whether the supplied challenge is currently activated on this character.
        /// </summary>
        bool IsChallengeActivated(ushort challengeId);

        /// <summary>
        /// Returns whether the supplied challenge is currently on cooldown for this character; false when the character has no record of it.
        /// </summary>
        bool IsChallengeOnCooldown(ushort challengeId);

        /// <summary>
        /// Returns whether the supplied challenge is unlocked for this character: the server tracks it (and sends <c>Unlocked = true</c> for it); false when the character has no record of it.
        /// </summary>
        bool IsChallengeUnlocked(ushort challengeId);

        /// <summary>
        /// Returns the current tier of the supplied challenge (the <c>CurrentTier</c> of <c>ServerChallengeUpdate</c>), or 0 when the character has no record of it.
        /// </summary>
        uint GetCurrentTier(ushort challengeId);
    }
}
