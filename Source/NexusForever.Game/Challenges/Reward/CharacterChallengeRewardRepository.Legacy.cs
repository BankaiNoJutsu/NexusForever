using Microsoft.EntityFrameworkCore;

namespace NexusForever.Game.Challenges.Reward
{
    public sealed partial class CharacterChallengeRewardRepository
    {
        // Persist a mapped count-based legacy tier without inventing a track
        // medal/receipt. Its direct item remains on the existing actor path.
        public CharacterChallengeAttemptState AdvanceLegacy(string attemptId, ushort challengeId, byte medal,
            CharacterChallengeState expected, CharacterChallengeState advanced)
            => Stage(() =>
            {
                var source = Source(challengeId);
                var attempt = ReadAttempt(attemptId, challengeId); ValidateAttempt(attempt, source);
                Require(attempt.RewardMode == (byte)CharacterChallengeRewardMode.LegacyDirect && medal < 3 && HasMedal(source, medal),
                    "Only a mapped legacy tier can advance without track credit.");
                Require(expected?.Activated == true && expected.AttemptId == attemptId && expected.Tier == medal
                    && advanced != null && advanced.CharacterId == characterId && advanced.ChallengeId == challengeId
                    && advanced.AttemptId == attemptId && advanced.LastRewardTier == medal
                    && (advanced.Activated ? advanced.Tier == medal + 1 && advanced.Completions == expected.Completions
                        : advanced.Tier == medal && advanced.Completions == checked(expected.Completions + 1)),
                    "Legacy tier transition changed its owner or completion history.");
                var previous = ReadChallenge(challengeId);
                if (CharacterChallengeState.FromModel(previous) == advanced)
                {
                    Require(attempt.Terminal == !advanced.Activated, "Legacy outcome lost its terminal attempt.");
                    return Attempt(attempt);
                }
                Require(!attempt.Terminal && CharacterChallengeState.FromModel(previous) == expected,
                    "Legacy baseline changed before tier advancement.");
                context.Attach(attempt); attempt.Terminal = !advanced.Activated;
                Own(attempt, () => context.CharacterChallengeAttempt.AsNoTracking()
                    .SingleOrDefault(row => row.Id == characterId && row.AttemptId == attemptId));
                StageChallenge(previous, advanced);
                return Attempt(attempt);
            });
    }
}
