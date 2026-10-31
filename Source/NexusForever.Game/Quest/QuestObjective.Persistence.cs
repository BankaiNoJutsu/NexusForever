using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Challenges.Reward;

namespace NexusForever.Game.Quest
{
    public partial class QuestObjective
    {
        private QuestRowPersistence<CharacterQuestObjectiveModel, ObjectiveStateCopy> persistence;
        internal bool NeedsPersistenceSave => persistence.NeedsSave;
        internal bool OwnsOtherSaveContext(CharacterContext context) => persistence.OwnsOtherContext(context);
        internal void ParentPersistenceDeleted() => persistence.ParentDeleted();
        internal void RestoreParentInContext(CharacterContext context) => persistence.RestoreParentInContext(context);
        private sealed record ObjectiveStateCopy(ulong Character, ushort Quest, byte Index, uint Progress, uint? Timer)
        {
            internal static ObjectiveStateCopy From(CharacterQuestObjectiveModel value) => value == null ? null : new(value.Id, value.QuestId, value.Index, value.Progress, value.Timer);
            internal CharacterQuestObjectiveModel Model() => new() { Id = Character, QuestId = Quest, Index = Index, Progress = Progress, Timer = Timer };
        }
        private void InitialisePersistence(CharacterQuestObjectiveModel baseline)
        {
            ulong character = player.CharacterId; ushort quest = (ushort)QuestInfo.Entry.Id; byte index = Index;
            persistence = new($"objective:{character}:{quest}:{index}", $"{character}:{quest}", ObjectiveStateCopy.From(baseline),
                context => context.CharacterQuestObjective.AsNoTracking().SingleOrDefault(row => row.Id == character && row.QuestId == quest && row.Index == index),
                ObjectiveStateCopy.From, value => value?.Model(), () => new(character, quest, index, progress, timer));
        }
        internal void RetireRewardOwner() => persistence.RetireRewardOwner();
        internal (bool Needed, Action<CharacterContext> Stage) CaptureNormalSave(object token)
        {
            var snapshot = persistence.CaptureNormal(token);
            return (snapshot.NeedsSave, context => persistence.Save(context, frozen: snapshot));
        }
        internal QuestRewardRow<CharacterChallengeQuestObjectiveState> CaptureRewardRow(object frozen = null) => persistence.CaptureReward(
            value => value == null ? null : new CharacterChallengeQuestObjectiveState(value.Character, value.Quest, value.Index, value.Progress, value.Timer),
            value => value == null ? null : new ObjectiveStateCopy(value.CharacterId, value.QuestId, value.Index, value.Progress, value.Timer), frozen);
    }
}
