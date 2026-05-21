using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Exile ship interior exploration quest.
    /// Quest 10520 → grants 10528 on completion.
    /// Objectives: checklist, kill creature 73499, reach cryopod zone.
    /// </summary>
    [ScriptFilterOwnerId(10520u)]
    public class Q10520ShipInteriorQuestScript : FollowUpQuestScript<Q10520ShipInteriorQuestScript>
    {
        protected override ushort NextQuestId => 10528;

        public Q10520ShipInteriorQuestScript(
            ILogger<Q10520ShipInteriorQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
