using NexusForever.Game.Challenges.Reward;

namespace NexusForever.Game.Quest
{
    // Actor-owned callbacks stay here. Worker preparation receives only copied
    // scalar candidates and targets, never this graph or its Quest references.
    internal sealed class QuestRewardRow<T>(T value, long revision, Func<IReadOnlyList<T>> candidates,
        Action<T> adopt, Action<T> rebase, Action<bool> retire) where T : class
    {
        internal T Value { get; } = value;
        internal long Revision { get; } = revision;
        internal IReadOnlyList<T> Candidates() => candidates();
        internal void Adopt(T actual) => adopt(actual);
        internal void Rebase(T actual) => rebase(actual);
        internal void Retire(bool value = true) => retire(value);
    }

    internal sealed class QuestRewardGraph(Quest quest, QuestRewardRow<CharacterChallengeQuestState> root,
        IReadOnlyDictionary<byte, QuestRewardRow<CharacterChallengeQuestObjectiveState>> objectives, bool deleted)
    {
        internal Quest Owner { get; } = quest;
        internal QuestRewardRow<CharacterChallengeQuestState> Root { get; } = root;
        internal IReadOnlyDictionary<byte, QuestRewardRow<CharacterChallengeQuestObjectiveState>> Objectives { get; } = objectives;
        internal CharacterChallengeQuestSnapshot Snapshot => deleted ? null : new(Root.Value, Objectives.OrderBy(p => p.Key).Select(p => p.Value.Value));
        internal bool ChangedFrom(QuestRewardGraph previous) => previous == null || !ReferenceEquals(Owner, previous.Owner)
            || !CharacterChallengeQuestSnapshot.Same(Snapshot, previous.Snapshot) || Root.Revision != previous.Root.Revision
            || Objectives.Count != previous.Objectives.Count || Objectives.Any(p => !previous.Objectives.TryGetValue(p.Key, out var old) || p.Value.Revision != old.Revision);
        internal void Adopt(CharacterChallengeQuestSnapshot stored)
        {
            Root.Adopt(stored?.Quest);
            var leaves = stored?.Objectives.ToDictionary(row => row.Index) ?? [];
            foreach (var row in Objectives) row.Value.Adopt(leaves.GetValueOrDefault(row.Key));
        }
        internal void Rebase(CharacterChallengeQuestSnapshot stored)
        {
            Root.Rebase(stored?.Quest); var leaves = stored?.Objectives.ToDictionary(row => row.Index) ?? [];
            foreach (var row in Objectives) row.Value.Rebase(leaves.GetValueOrDefault(row.Key));
        }
        internal void Retire(bool value = true) { Root.Retire(value); foreach (var row in Objectives.Values) row.Retire(value); }
    }
}
