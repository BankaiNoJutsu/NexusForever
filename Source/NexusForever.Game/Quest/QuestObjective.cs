using NexusForever.Database.Character;
using System.Numerics;
using NexusForever.Game;
using NexusForever.Game.Abstract;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable.Model;
using NexusForever.Shared.Game;

namespace NexusForever.Game.Quest
{
    public partial class QuestObjective : IQuestObjective
    {
        [Flags]
        public enum QuestObjectiveSaveMask
        {
            None     = 0x00,
            Create   = 0x01,
            Progress = 0x02,
            Timer    = 0x04
        }

        public IQuestInfo QuestInfo { get; }
        public IQuestObjectiveInfo ObjectiveInfo { get; }

        public byte Index { get; }

        public uint Progress
        {
            get => progress;
            set
            {
                lock (persistence.Gate) { progress = value; persistence.Dirty((int)QuestObjectiveSaveMask.Progress); }
                player.RequestSave();
            }
        }

        private uint progress;

        public uint? Timer
        {
            get => timer;
            set
            {
                lock (persistence.Gate) { timer = value; persistence.Dirty((int)QuestObjectiveSaveMask.Timer); }
            }
        }

        private uint? timer;

        private List<uint> targetIds = [];

        private readonly IPlayer player;
        private readonly IAssetManager assetManager;
        private UpdateTimer objectiveTimer;

        /// <summary>
        /// Create a new <see cref="IQuestObjective"/> from an existing database model.
        /// </summary>
        public QuestObjective(IPlayer owner, IQuestInfo questInfo, IQuestObjectiveInfo objectiveInfo, CharacterQuestObjectiveModel model, IAssetManager assetManager = null)
        {
            player            = owner;
            this.assetManager = assetManager;

            QuestInfo     = questInfo;
            ObjectiveInfo = objectiveInfo;
            Index         = model.Index;
            progress      = model.Progress;
            timer         = model.Timer;
            InitialisePersistence(model);

            if (timer != null)
                objectiveTimer = new UpdateTimer(timer.Value / 1000d);

            if (IsChecklist() || UsesTargetGroups())
                BuildTargets();
        }

        /// <summary>
        /// Create a new <see cref="IQuestObjective"/> from supplied <see cref="QuestObjectiveEntry"/>.
        /// </summary>
        public QuestObjective(IPlayer owner, IQuestInfo questInfo, IQuestObjectiveInfo objectiveInfo, byte index, IAssetManager assetManager = null)
        {
            player            = owner;
            this.assetManager = assetManager;

            QuestInfo     = questInfo;
            ObjectiveInfo = objectiveInfo;
            Index         = index;
            InitialisePersistence(null);

            if (objectiveInfo.Entry.MaxTimeAllowedMS != 0u)
            {
                objectiveTimer = new UpdateTimer(objectiveInfo.Entry.MaxTimeAllowedMS / 1000d);
                Timer = objectiveInfo.Entry.MaxTimeAllowedMS;
            }

            if (IsChecklist() || UsesTargetGroups())
                BuildTargets();

            persistence.Dirty((int)QuestObjectiveSaveMask.Create);
        }

        /// <summary>
        /// Builds the target ID list for this <see cref="IQuestObjective"/>.
        /// </summary>
        private void BuildTargets()
        {
            targetIds = (assetManager?.GetQuestObjectiveTargetIds(ObjectiveInfo.Id) ?? Enumerable.Empty<uint>()).ToList();
        }

        public void Save(CharacterContext context) => persistence.Save(context);

        public void Update(double lastTick)
        {
            if (objectiveTimer == null || IsComplete())
                return;

            objectiveTimer.Update(lastTick);
            Timer = (uint)(objectiveTimer.Time * 1000d);
        }

        private bool IsDynamic()
        {
            // Dynamic objectives store client progress on a 0-1000 scale. Checklist
            // objectives keep progress as bit flags even when the table flag is set.
            if (IsChecklist())
                return false;

            // SucceedCSI rows such as Q3479/Q3480 trapped survivors carry the dynamic
            // flag, but the client still compares the wire progress to the row count.
            if (ObjectiveInfo.Type == QuestObjectiveType.SucceedCSI)
                return false;

            return ObjectiveInfo.Entry.Count > 1u
                && !ObjectiveInfo.DisablesDynamicProgress()
                && (ObjectiveInfo.UsesDynamicProgress() || IsDynamicObjectiveType());
        }

        private bool IsDynamicObjectiveType()
        {
            return ObjectiveInfo.Type is QuestObjectiveType.KillCreature
                    or QuestObjectiveType.KillTargetGroups
                    or QuestObjectiveType.KillNamedCreature
                    or QuestObjectiveType.KillTargetGroup
                    or QuestObjectiveType.KillCreature2;
        }

        /// <summary>
        /// Return if progress is a bit mask of creature checklist indices rather than a count.
        /// </summary>
        /// <remarks>
        /// The client treats objective types 10, 13, 14 and 18 as checklists (Quest_IsChecklistObjectiveType 1405517a0) in three places: prerequisite type 94, the item-use check and
        /// the completed count the quest tracker shows. Types 13 and 18 kept a count here until 2026-11-10; see Decomp/Analysis/PREREQUISITE_CHECK_REVIEW_20261110.md.
        /// Types 27 and 28 are checklists natively for two activate modes and are not modelled. Evidence tier: native bodies. No retail parity claim.
        /// </remarks>
        private bool IsChecklist() => IsChecklistType(ObjectiveInfo.Type);

        public static bool IsChecklistType(QuestObjectiveType type)
        {
            return type is QuestObjectiveType.ActivateTargetGroupChecklist
                or QuestObjectiveType.ScriptedTargetGroupChecklist
                or QuestObjectiveType.SpellSuccess2
                or QuestObjectiveType.TalkToTargetGroup;
        }

        private bool UsesTargetGroups()
        {
            return ObjectiveInfo.Type is QuestObjectiveType.ActivateTargetGroup
                    or QuestObjectiveType.ActivateTargetGroupChecklist
                    or QuestObjectiveType.KillTargetGroup
                    or QuestObjectiveType.KillTargetGroups
                    or QuestObjectiveType.TalkToTargetGroup
                    or QuestObjectiveType.ScriptedTargetGroupChecklist
                || ObjectiveInfo.Type == QuestObjectiveType.ActivateEntity && ObjectiveInfo.Entry.TargetGroupIdRewardPane != 0u;
        }

        /// <summary>
        /// Return if the objective has been completed.
        /// </summary>
        public bool IsComplete()
        {
            if (IsChecklist())
                return BitOperations.PopCount(progress) >= GetMaxValue();

            return progress >= GetMaxValue();
        }

        /// <summary>
        /// Return if the objective can be updated by the supplied target id.
        /// </summary>
        public bool IsTarget(uint id)
        {
            return ObjectiveInfo.Entry.Data == id || targetIds.Contains(id);
        }

        private uint GetMaxValue()
        {
            return IsDynamic() ? 1000u : ObjectiveInfo.Entry.Count;
        }

        /// <summary>
        /// Update object progress with supplied update.
        /// </summary>
        public void ObjectiveUpdate(uint update)
        {
            if (IsChecklist())
            {
                if (update >= sizeof(uint) * 8)
                    return;

                Progress = progress | (1u << (int)update);
                return;
            }

            if (IsDynamic())
            {
                ulong requiredCount = ObjectiveInfo.Entry.Count;
                update = (uint)Math.Min(1000ul, ((ulong)update * 1000ul + requiredCount - 1ul) / requiredCount);
            }

            Progress = Math.Min(progress + update, GetMaxValue());
        }

       
        public void Complete()
        {
            if (IsChecklist())
            {
                uint update = 0u;
                for (int i = 0; i < GetMaxValue() && i < sizeof(uint) * 8; i++)
                    update |= 1u << i;

                Progress = update;
                return;
            }

            Progress = GetMaxValue();
        }
    }
}
