using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Quest 3886 (Fiery Distraction) - grants 3673 (Contact with Thayd) on completion.
    /// Objectives: SucceedCSI on creature 13630, ActivateTargetGroupChecklist tg=1460 x3.
    /// </summary>
    [ScriptFilterOwnerId(3886u)]
    public class Q3886FieryDistractionQuestScript : FollowUpQuestScript<Q3886FieryDistractionQuestScript>
    {
        protected override ushort NextQuestId => 3673;

        public Q3886FieryDistractionQuestScript(
            ILogger<Q3886FieryDistractionQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
