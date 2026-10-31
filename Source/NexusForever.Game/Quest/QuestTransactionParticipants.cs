using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Challenges.Reward;

namespace NexusForever.Game.Quest
{
    // Scalar Quest participants shared by reward transaction owners. This helper
    // creates no Challenge authority and retains no actor callback.
    internal sealed class QuestTransactionParticipants
    {
        private readonly CharacterContext context;
        private readonly ulong characterId;
        private readonly IReadOnlyList<CharacterChallengeQuestChange> questChanges;
        private readonly List<(ushort Quest, CharacterChallengeQuestSnapshot Expected)> questWitnesses = [];
        internal QuestTransactionParticipants(CharacterContext context, ulong characterId, IReadOnlyList<CharacterChallengeQuestChange> changes)
        {
            this.context = context; this.characterId = characterId;
            questChanges = Array.AsReadOnly((changes ?? throw new ArgumentNullException(nameof(changes))).ToArray());
            Require(questChanges.All(row => row != null && row.CharacterId == characterId)
                && questChanges.Select(row => row.QuestId).Distinct().Count() == questChanges.Count,
                "Quest participants have conflicting ownership.");
        }
        internal IReadOnlyDictionary<ushort, CharacterChallengeQuestSnapshot> ReadQuestParticipantsForExecution()
            => new System.Collections.ObjectModel.ReadOnlyDictionary<ushort, CharacterChallengeQuestSnapshot>(
                questChanges.ToDictionary(change => change.QuestId, change => ReadQuest(change.QuestId)));
        // Recovery never replays a broad objective event. It checks the exact
        // before graph if the reward is absent, or the after graph if present.
        internal void VerifyQuestChanges(IReadOnlyList<CharacterChallengeQuestChange> changes, bool applied)
        {
            foreach (var change in changes)
            {
                Require(change.CharacterId == characterId, "Quest participant belongs to another character.");
                var expected = EncodeQuest(applied ? change.After : change.Before);
                Require(CharacterChallengeQuestSnapshot.Same(ReadQuest(change.QuestId), expected),
                    "Quest graph changed outside the retained reward snapshot.");
            }
        }

        internal CharacterChallengeQuestSnapshot ReadQuest(ushort id)
        {
            var root = context.CharacterQuest.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.QuestId == id);
            var leaves = context.CharacterQuestObjective.AsNoTracking().Where(row => row.Id == characterId && row.QuestId == id)
                .OrderBy(row => row.Index).AsEnumerable().Select(CharacterChallengeQuestObjectiveState.FromModel).ToArray();
            Require(root != null || leaves.Length == 0, "Quest objectives have no parent.");
            return root == null ? null : new(CharacterChallengeQuestState.FromModel(root), leaves);
        }

        internal CharacterChallengeQuestSnapshot EncodeQuest(CharacterChallengeQuestSnapshot value)
        {
            if (value?.Quest.Reset is not { } instant || context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) != true)
                return value;
            // Match the existing provider's DATETIME conversion, including its
            // session sql_mode, rather than silently imposing a rounding policy.
            var stored = context.Database.SqlQueryRaw<DateTime>("SELECT CAST({0} AS DATETIME) AS Value", instant).AsEnumerable().Single();
            return new(value.Quest with { Reset = stored }, value.Objectives);
        }

        internal void StageQuestChanges(bool apply, Action<object, Func<object>> own)
        {
            foreach (var change in questChanges.OrderBy(row => row.QuestId))
            {
                var before = EncodeQuest(change.Before);
                var after = EncodeQuest(change.After);
                Require(CharacterChallengeQuestSnapshot.Same(ReadQuest(change.QuestId), apply ? before : after),
                    "Quest graph does not match this completion's exact baseline or replay witness.");
                questWitnesses.Add((change.QuestId, after));
                if (!apply) continue;

                CharacterQuestModel root = (before ?? after).Quest.ToModel();
                var rootEntry = context.Entry(root);
                rootEntry.State = before == null ? EntityState.Added : after == null ? EntityState.Deleted : EntityState.Unchanged;
                if (before != null && after != null) rootEntry.CurrentValues.SetValues(after.Quest.ToModel());
                ushort id = change.QuestId;
                own(root, () => context.CharacterQuest.AsNoTracking().SingleOrDefault(row => row.Id == characterId && row.QuestId == id));

                var oldRows = before?.Objectives.ToDictionary(row => row.Index) ?? [];
                var newRows = after?.Objectives.ToDictionary(row => row.Index) ?? [];
                foreach (byte index in oldRows.Keys.Union(newRows.Keys).Order())
                {
                    oldRows.TryGetValue(index, out var oldRow); newRows.TryGetValue(index, out var newRow);
                    var leaf = (oldRow ?? newRow).ToModel(); var entry = context.Entry(leaf);
                    // Ordinal zero is a valid existing composite key.
                    entry.State = oldRow == null ? EntityState.Added : newRow == null ? EntityState.Deleted : EntityState.Unchanged;
                    if (oldRow == null)
                    {
                        // The schema declares a default for the ordinal. EF can
                        // otherwise assign zero a temporary generated key, which
                        // is not the identity captured by this immutable command.
                        entry.Property(row => row.Index).CurrentValue = index;
                        entry.Property(row => row.Index).IsTemporary = false;
                    }
                    if (oldRow != null && newRow != null) entry.CurrentValues.SetValues(newRow.ToModel());
                    own(leaf, () => context.CharacterQuestObjective.AsNoTracking()
                        .SingleOrDefault(row => row.Id == characterId && row.QuestId == id && row.Index == index));
                }
            }
        }

        internal void ValidateQuestWitnesses()
        {
            foreach (var witness in questWitnesses)
                Require(CharacterChallengeQuestSnapshot.Same(ReadQuest(witness.Quest), witness.Expected),
                    "Reward transaction no longer contains its complete Quest graph.");
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
