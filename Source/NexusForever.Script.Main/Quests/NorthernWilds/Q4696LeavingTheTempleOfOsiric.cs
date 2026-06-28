using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds/Galeras transition: Leaving the Temple of Osiric.
    /// </summary>
    [ScriptFilterOwnerId(4696u)]
    public class Q4696LeavingTheTempleOfOsiricQuestScript : FollowUpQuestScript<Q4696LeavingTheTempleOfOsiricQuestScript>
    {
        protected override ushort NextQuestId => 4694;

        public Q4696LeavingTheTempleOfOsiricQuestScript(
            ILogger<Q4696LeavingTheTempleOfOsiricQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
