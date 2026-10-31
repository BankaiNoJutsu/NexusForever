using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Challenges.Reward
{
    // Scalar copies only. Actor ownership must be retained separately until the
    // executor result is adopted; a quest ID is not an acceptance identity.
    internal sealed record CharacterChallengeQuestState(ulong CharacterId, ushort QuestId,
        byte State, byte Flags, uint? Timer, DateTime? Reset, string ContractAcceptanceId = null)
    {
        internal static CharacterChallengeQuestState FromModel(CharacterQuestModel value) => value == null ? null
            : new(value.Id, value.QuestId, value.State, value.Flags, value.Timer, value.Reset, value.ContractAcceptanceId);
        internal CharacterQuestModel ToModel() => new()
            { Id = CharacterId, QuestId = QuestId, State = State, Flags = Flags, Timer = Timer, Reset = Reset, ContractAcceptanceId = ContractAcceptanceId };
    }

    internal sealed record CharacterChallengeQuestObjectiveState(ulong CharacterId, ushort QuestId,
        byte Index, uint Progress, uint? Timer)
    {
        internal static CharacterChallengeQuestObjectiveState FromModel(CharacterQuestObjectiveModel value)
            => new(value.Id, value.QuestId, value.Index, value.Progress, value.Timer);
        internal CharacterQuestObjectiveModel ToModel() => new()
            { Id = CharacterId, QuestId = QuestId, Index = Index, Progress = Progress, Timer = Timer };
    }

    internal sealed class CharacterChallengeQuestSnapshot
    {
        internal CharacterChallengeQuestState Quest { get; }
        internal IReadOnlyList<CharacterChallengeQuestObjectiveState> Objectives { get; }

        internal CharacterChallengeQuestSnapshot(CharacterChallengeQuestState quest,
            IEnumerable<CharacterChallengeQuestObjectiveState> objectives)
        {
            ArgumentNullException.ThrowIfNull(quest);
            ArgumentNullException.ThrowIfNull(objectives);
            var rows = objectives.ToArray();
            if (quest.CharacterId == 0 || quest.QuestId == 0
                || quest.ContractAcceptanceId != null && !NexusForever.Game.Quest.Quest.ValidContractAcceptanceId(quest.ContractAcceptanceId)
                || rows.Any(row => row == null
                || row.CharacterId != quest.CharacterId || row.QuestId != quest.QuestId)
                || rows.Select(row => row.Index).Distinct().Count() != rows.Length)
                throw new ArgumentException("Quest snapshots require one complete, uniquely indexed owner graph.");
            Quest = quest;
            Objectives = Array.AsReadOnly(rows.OrderBy(row => row.Index).ToArray());
        }

        internal static bool Same(CharacterChallengeQuestSnapshot first, CharacterChallengeQuestSnapshot second)
            => ReferenceEquals(first, second) || first != null && second != null
                && first.Quest == second.Quest && first.Objectives.SequenceEqual(second.Objectives);
    }

    internal sealed class CharacterChallengeQuestChange
    {
        internal CharacterChallengeQuestSnapshot Before { get; }
        internal CharacterChallengeQuestSnapshot After { get; }
        internal ulong CharacterId => (Before ?? After).Quest.CharacterId;
        internal ushort QuestId => (Before ?? After).Quest.QuestId;

        internal CharacterChallengeQuestChange(CharacterChallengeQuestSnapshot before, CharacterChallengeQuestSnapshot after)
        {
            if (before == null && after == null || before != null && after != null
                && (before.Quest.CharacterId != after.Quest.CharacterId || before.Quest.QuestId != after.Quest.QuestId))
                throw new ArgumentException("Quest changes cannot replace their owner identity.");
            Before = before; After = after;
        }
    }
}
