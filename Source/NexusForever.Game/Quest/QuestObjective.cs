using Microsoft.EntityFrameworkCore.ChangeTracking;
using NexusForever.Database.Character;
using System.Numerics;
using NexusForever.Game;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Quest
{
    public class QuestObjective : IQuestObjective
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
                saveMask |= QuestObjectiveSaveMask.Progress;
                progress = value;
                player.RequestSave();
            }
        }

        private uint progress;

        public uint? Timer
        {
            get => timer;
            set
            {
                saveMask |= QuestObjectiveSaveMask.Timer;
                timer = value;
            }
        }

        private uint? timer;

        private List<uint> targetIds = [];

        private QuestObjectiveSaveMask saveMask;

        private readonly IPlayer player;

        /// <summary>
        /// Create a new <see cref="IQuestObjective"/> from an existing database model.
        /// </summary>
        public QuestObjective(IPlayer owner, IQuestInfo questInfo, IQuestObjectiveInfo objectiveInfo, CharacterQuestObjectiveModel model)
        {
            player        = owner;

            QuestInfo     = questInfo;
            ObjectiveInfo = objectiveInfo;
            Index         = model.Index;
            progress      = model.Progress;
            timer         = model.Timer;

            if (IsChecklist() || UsesTargetGroups())
                BuildTargets();
        }

        /// <summary>
        /// Create a new <see cref="IQuestObjective"/> from supplied <see cref="QuestObjectiveEntry"/>.
        /// </summary>
        public QuestObjective(IPlayer owner, IQuestInfo questInfo, IQuestObjectiveInfo objectiveInfo, byte index)
        {
            player        = owner;

            QuestInfo     = questInfo;
            ObjectiveInfo = objectiveInfo;
            Index         = index;

            if (objectiveInfo.Entry.MaxTimeAllowedMS != 0u)
            {
                // TODO
            }

            if (IsChecklist() || UsesTargetGroups())
                BuildTargets();

            saveMask = QuestObjectiveSaveMask.Create;
        }

        /// <summary>
        /// Builds the target ID list for this <see cref="IQuestObjective"/>.
        /// </summary>
        private void BuildTargets()
        {
            targetIds = AssetManager.Instance.GetQuestObjectiveTargetIds(ObjectiveInfo.Id).ToList();
        }

        public void Save(CharacterContext context)
        {
            if (saveMask == QuestObjectiveSaveMask.None)
                return;

            if ((saveMask & QuestObjectiveSaveMask.Create) != 0)
            {
                context.Add(new CharacterQuestObjectiveModel
                {
                    Id       = player.CharacterId,
                    QuestId  = (ushort)QuestInfo.Entry.Id,
                    Index    = Index,
                    Progress = Progress
                });
            }
            else
            {
                var model = new CharacterQuestObjectiveModel
                {
                    Id      = player.CharacterId,
                    QuestId = (ushort)QuestInfo.Entry.Id,
                    Index   = Index
                };

                EntityEntry<CharacterQuestObjectiveModel> entity = context.Entry(model);
                if ((saveMask & QuestObjectiveSaveMask.Progress) != 0)
                {
                    model.Progress = Progress;
                    entity.Property(p => p.Progress).IsModified = true;
                }

                if ((saveMask & QuestObjectiveSaveMask.Timer) != 0)
                {
                    // TODO
                }
            }

            saveMask = QuestObjectiveSaveMask.None;
        }

        public void Update(double lastTick)
        {
            // TODO: update timer
        }

        private bool IsDynamic()
        {
            // dynamic objectives have their progress based on percentage rather than count
            return ObjectiveInfo.Type is QuestObjectiveType.KillCreature
                    or QuestObjectiveType.KillTargetGroups
                    or QuestObjectiveType.Unknown15
                    or QuestObjectiveType.KillTargetGroup
                    or QuestObjectiveType.KillCreature2
                && ObjectiveInfo.Entry.Count > 1u
                && !ObjectiveInfo.HasUnknown0200();
        }

        private bool IsChecklist()
        {
            return ObjectiveInfo.Type is QuestObjectiveType.ActivateTargetGroupChecklist
                or QuestObjectiveType.Unknown10;
        }

        private bool UsesTargetGroups()
        {
            return ObjectiveInfo.Type is QuestObjectiveType.ActivateTargetGroup
                    or QuestObjectiveType.ActivateTargetGroupChecklist
                    or QuestObjectiveType.KillTargetGroup
                    or QuestObjectiveType.KillTargetGroups
                    or QuestObjectiveType.TalkToTargetGroup
                    or QuestObjectiveType.Unknown10
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
