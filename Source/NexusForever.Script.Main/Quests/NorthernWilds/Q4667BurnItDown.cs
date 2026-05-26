using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds/Galeras transition: Burn It Down -> Leaving the Temple of Osiric.
    /// </summary>
    [ScriptFilterOwnerId(4667u)]
    public class Q4667BurnItDownQuestScript : FollowUpQuestScript<Q4667BurnItDownQuestScript>
    {
        protected override ushort NextQuestId => 4696;

        public Q4667BurnItDownQuestScript(
            ILogger<Q4667BurnItDownQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
