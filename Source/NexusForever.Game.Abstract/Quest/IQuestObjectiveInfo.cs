using NexusForever.Game.Static.Quest;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Abstract.Quest
{
    public interface IQuestObjectiveInfo
    {
        uint Id { get; }
        QuestObjectiveType Type { get; }

        QuestObjectiveEntry Entry { get; }

        /// <summary>
        /// Quest objective is sequential, previous quest objective must be complete.
        /// </summary>
        bool IsSequential();

        bool RequiresPreviousObjectives();

        /// <summary>
        /// Quest objective is hidden until previous objective is complete.
        /// </summary>
        bool IsHidden();

        /// <summary>
        /// Quest objective is optional, doesn't need to be complete for quest to be achieved.
        /// </summary>
        bool IsOptional();

        bool UsesDynamicProgress();

        bool DisablesDynamicProgress();

        /// <summary>
        /// Objective type 27 or 28 whose PathSoldierActivate row (<c>Data</c>) has activate mode 1 or 2: progress is a bit mask of creature checklist indices.
        /// </summary>
        bool IsSoldierActivateChecklist => false;
    }
}
