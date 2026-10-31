using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Challenges.Reward;

namespace NexusForever.Game.Quest
{
    public partial class Quest
    {
        private QuestRowPersistence<CharacterQuestModel, QuestStateCopy> persistence;
        private bool deleteRequested;
        private readonly HashSet<CharacterContext> deletionObservers = [];
        internal bool IsPersistenceDeleted => persistence.IsAbsent;
        internal bool NeedsPersistenceSave => persistence.NeedsSave || !deleteRequested && objectives.Any(objective => objective is not QuestObjective concrete || concrete.NeedsPersistenceSave);
        private sealed record QuestStateCopy(ulong Character, ushort Quest, byte State, byte Flags, uint? Timer, DateTime? Reset, string ContractAcceptance)
        {
            internal static QuestStateCopy From(CharacterQuestModel value) => value == null ? null : new(value.Id, value.QuestId, value.State, value.Flags, value.Timer, value.Reset, value.ContractAcceptanceId);
            internal CharacterQuestModel Model() => new() { Id = Character, QuestId = Quest, State = State, Flags = Flags, Timer = Timer, Reset = Reset, ContractAcceptanceId = ContractAcceptance };
        }
        private void InitialisePersistence(CharacterQuestModel baseline)
        {
            ulong character = player.CharacterId; ushort quest = Id;
            persistence = new($"quest:{character}:{quest}", $"{character}:{quest}", QuestStateCopy.From(baseline),
                context => context.CharacterQuest.AsNoTracking().SingleOrDefault(row => row.Id == character && row.QuestId == quest),
                QuestStateCopy.From, value => value?.Model(), () => new(character, quest, (byte)state, (byte)flags, timer, reset, contractAcceptanceId), ResetEncoding);
        }

        private static (Func<QuestStateCopy, QuestStateCopy> Stage, Func<QuestStateCopy, QuestStateCopy> Ack) ResetEncoding(CharacterContext context)
        {
            if (context.Database.ProviderName?.Contains("MySql", StringComparison.Ordinal) != true) return (value => value, value => value);
            var dates = new Dictionary<DateTime, DateTime>();
            QuestStateCopy Encode(QuestStateCopy value, bool allowRead)
            {
                if (value?.Reset is not { } instant) return value;
                if (!dates.TryGetValue(instant, out var stored))
                {
                    // The current reset column is DATETIME without fractional
                    // precision. Ask this session to perform the same conversion
                    // instead of assuming MySQL/MariaDB rounding or sql_mode.
                    if (!allowRead) return value; // a later actor mutation remains dirty
                    stored = context.Database.SqlQueryRaw<DateTime>("SELECT CAST({0} AS DATETIME) AS Value", instant).AsEnumerable().Single();
                    dates.Add(instant, stored);
                }
                return value with { Reset = stored };
            }
            // Settlement may run after context disposal: Ack only consults the
            // captured conversion and never queries SQL.
            return (value => Encode(value, true), value => Encode(value, false));
        }
        private void SavePersistence(CharacterContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            try { persistence.AssertRewardOwner(); }
            catch (Exception error) { context.RejectSave(error); throw; }
            if (deleteRequested && persistence.IsAbsent) return;
            if (!persistence.NeedsSave && !deleteRequested && !objectives.Any(objective => objective is not QuestObjective concrete || concrete.NeedsPersistenceSave)) return;
            foreach (var objective in objectives.OfType<QuestObjective>())
                if (objective.OwnsOtherSaveContext(context))
                {
                    var error = new InvalidOperationException("Quest objective has an unfinished save unit."); context.RejectSave(error); throw error;
                }
            bool restoringParent = !deleteRequested && persistence.HasFlushedDeletion(context);
            persistence.Save(context, deleteRequested, force: true);
            if (restoringParent) foreach (var objective in objectives.OfType<QuestObjective>()) objective.RestoreParentInContext(context);
            if (!deleteRequested) foreach (var objective in objectives) objective.Save(context);
            ObservePersistenceDeletion(context);
        }
        private void ObservePersistenceDeletion(CharacterContext context)
        {
            lock (persistence.Gate)
            {
                if (!deletionObservers.Add(context)) return;
                context.OnSaveSettled(committed =>
                {
                    lock (persistence.Gate)
                    {
                        deletionObservers.Remove(context);
                        if (committed && persistence.IsAbsent)
                            foreach (var objective in objectives.OfType<QuestObjective>()) objective.ParentPersistenceDeleted();
                    }
                });
            }
        }
        internal void RetireRewardOwner()
        {
            persistence.RetireRewardOwner();
            foreach (var objective in objectives.OfType<QuestObjective>()) objective.RetireRewardOwner();
        }
        internal QuestNormalSaveGraph CaptureNormalSave(object token)
        {
            var root = persistence.CaptureNormal(token);
            var leaves = objectives.Select(objective => objective is QuestObjective concrete ? concrete.CaptureNormalSave(token)
                : throw new InvalidOperationException("Frozen Quest save requires concrete objectives.")).ToArray();
            bool deleted = PendingDelete;
            bool needed = deleted ? !persistence.IsAbsent : root.NeedsSave || leaves.Any(leaf => leaf.Needed);
            return new(this, needed, deleted, context =>
            {
                if (!needed) return;
                persistence.Save(context, deleted, force: true, frozen: root);
                if (!deleted) foreach (var leaf in leaves) leaf.Stage(context);
                ObservePersistenceDeletion(context);
            }, deleted ? null : CaptureRewardGraph(token).Snapshot);
        }
        internal QuestRewardGraph CaptureRewardGraph(object frozen = null) => new(this,
            persistence.CaptureReward(value => value == null ? null : new CharacterChallengeQuestState(value.Character, value.Quest, value.State, value.Flags, value.Timer, value.Reset, value.ContractAcceptance),
                value => value == null ? null : new QuestStateCopy(value.CharacterId, value.QuestId, value.State, value.Flags, value.Timer, value.Reset, value.ContractAcceptanceId), frozen),
            objectives.Select(objective => objective is QuestObjective concrete ? concrete.CaptureRewardRow(frozen)
                : throw new InvalidOperationException("Quest reward requires concrete objective ownership.")).ToDictionary(row => row.Value.Index), PendingDelete);
    }
}
