using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Dominion post-combat quest: talk to three NPCs on the ship deck before departure.
    /// Quest 10522 → grants 10523 on completion.
    /// Objectives: SucceedCSI on creatures 73498, 74745, 74746.
    /// </summary>
    [ScriptFilterOwnerId(10522u)]
    public class Q10522PreparingForDepartureQuestScript : FollowUpQuestScript<Q10522PreparingForDepartureQuestScript>
    {
        protected override ushort NextQuestId => 10523;

        public Q10522PreparingForDepartureQuestScript(
            ILogger<Q10522PreparingForDepartureQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
