using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Quest 3479 (From the Wreckage) - Exile faction start.
    /// Mutually exclusive with Q3480 (Reporting for Duty).
    /// Grants 3667 (The Tower) on completion.
    /// Objectives: SucceedCSI x3 (creature 11070), KillTargetGroup x8 (tg 7288).
    /// </summary>
    [ScriptFilterOwnerId(3479u)]
    public class Q3479FromTheWreckageQuestScript : FollowUpQuestScript<Q3479FromTheWreckageQuestScript>
    {
        protected override ushort NextQuestId => 3667;

        public Q3479FromTheWreckageQuestScript(
            ILogger<Q3479FromTheWreckageQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
