using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Dominion ship interior exploration quest.
    /// Quest 10523 → grants 10530 on completion.
    /// Objectives: checklist, kill creature 73499, reach cryopod zone.
    /// </summary>
    [ScriptFilterOwnerId(10523u)]
    public class Q10523ShipInteriorQuestScript : FollowUpQuestScript<Q10523ShipInteriorQuestScript>
    {
        protected override ushort NextQuestId => 10530;

        public Q10523ShipInteriorQuestScript(
            ILogger<Q10523ShipInteriorQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
