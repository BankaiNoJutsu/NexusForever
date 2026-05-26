using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds/Galeras transition: Take the Fight to Them -> Burn It Down.
    /// </summary>
    [ScriptFilterOwnerId(4666u)]
    public class Q4666TakeTheFightToThemQuestScript : FollowUpQuestScript<Q4666TakeTheFightToThemQuestScript>
    {
        protected override ushort NextQuestId => 4667;

        public Q4666TakeTheFightToThemQuestScript(
            ILogger<Q4666TakeTheFightToThemQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
