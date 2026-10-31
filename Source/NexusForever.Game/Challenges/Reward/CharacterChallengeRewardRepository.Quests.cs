using NexusForever.Game.Quest;

namespace NexusForever.Game.Challenges.Reward
{
    public sealed partial class CharacterChallengeRewardRepository
    {
        private IReadOnlyList<CharacterChallengeQuestChange> questChanges = Array.Empty<CharacterChallengeQuestChange>();
        private QuestTransactionParticipants questParticipants;
        private bool questsBound, challengeChanged;
        private QuestTransactionParticipants Participants => questParticipants ??= new(context, characterId, questChanges);
        internal IReadOnlyDictionary<ushort, CharacterChallengeQuestSnapshot> ReadQuestParticipantsForExecution()
            => Participants.ReadQuestParticipantsForExecution();
        internal void BindQuestChanges(IReadOnlyList<CharacterChallengeQuestChange> changes)
        {
            try
            {
                Require(!started && !questsBound, "Quest participants must be bound before staging a single reward operation.");
                questsBound = true;
                questChanges = Array.AsReadOnly((changes ?? throw new ArgumentNullException(nameof(changes))).ToArray());
                questParticipants = new(context, characterId, questChanges);
            }
            catch (Exception error) { context.RejectSave(error); throw; }
        }
        internal void VerifyQuestChanges(IReadOnlyList<CharacterChallengeQuestChange> changes, bool applied)
            => new QuestTransactionParticipants(context, characterId, changes).VerifyQuestChanges(changes, applied);
        private void StageQuestChanges() => Participants.StageQuestChanges(challengeChanged, Own);
        private void ValidateQuestWitnesses() => Participants.ValidateQuestWitnesses();
    }
}
