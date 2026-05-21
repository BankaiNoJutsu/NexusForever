using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Exile post-combat quest: talk to three NPCs on the ship deck before departure.
    /// Quest 10519 → grants 10520 on completion.
    /// Objectives: SucceedCSI on creatures 73498, 74745, 74746.
    /// </summary>
    [ScriptFilterOwnerId(10519u)]
    public class Q10519PreparingForDepartureQuestScript : FollowUpQuestScript<Q10519PreparingForDepartureQuestScript>
    {
        protected override ushort NextQuestId => 10520;

        public Q10519PreparingForDepartureQuestScript(
            ILogger<Q10519PreparingForDepartureQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
