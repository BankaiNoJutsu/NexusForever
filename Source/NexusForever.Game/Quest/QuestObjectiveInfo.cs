using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Quest
{
    public class QuestObjectiveInfo : IQuestObjectiveInfo
    {
        public uint Id => Entry.Id;
        public QuestObjectiveType Type => (QuestObjectiveType)Entry.Type;

        public QuestObjectiveEntry Entry { get; }

        /// <summary>
        /// Create a new <see cref="QuestObjectiveInfo"/> with supplied <see cref="QuestObjectiveEntry"/>.
        /// </summary>
        public QuestObjectiveInfo(QuestObjectiveEntry entry, bool soldierActivateChecklist = false)
        {
            Entry = entry;
            IsSoldierActivateChecklist = soldierActivateChecklist;
        }

        /// <summary>
        /// Objective type 27 or 28 whose PathSoldierActivate row (<c>Data</c>) has activate mode 1 or 2: progress is a bit mask of creature checklist indices.
        /// </summary>
        public bool IsSoldierActivateChecklist { get; }

        /// <summary>
        /// Quest objective is sequential, previous quest objective must be complete.
        /// </summary>
        public bool IsSequential()
        {
            return ((QuestObjectiveFlags)Entry.Flags & QuestObjectiveFlags.Sequential) != 0;
        }

        public bool RequiresPreviousObjectives()
        {
            const QuestObjectiveFlags flags =
                QuestObjectiveFlags.RequiresPreviousObjectives
                | QuestObjectiveFlags.Sequential
                | QuestObjectiveFlags.Hidden;

            return ((QuestObjectiveFlags)Entry.Flags & flags) != 0;
        }

        /// <summary>
        /// Quest objective is hidden until previous objective is complete.
        /// </summary>
        public bool IsHidden()
        {
            return ((QuestObjectiveFlags)Entry.Flags & QuestObjectiveFlags.Hidden) != 0;
        }

        /// <summary>
        /// Quest objective is optional, doesn't need to be complete for quest to be achieved.
        /// </summary>
        public bool IsOptional()
        {
            return ((QuestObjectiveFlags)Entry.Flags & QuestObjectiveFlags.Optional) != 0;
        }

        public bool UsesDynamicProgress()
        {
            return ((QuestObjectiveFlags)Entry.Flags & QuestObjectiveFlags.UsesDynamicProgress) != 0;
        }

        public bool DisablesDynamicProgress()
        {
            return ((QuestObjectiveFlags)Entry.Flags & QuestObjectiveFlags.DisablesDynamicProgress) != 0;
        }
    }
}
