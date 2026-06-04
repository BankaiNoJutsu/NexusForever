using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds: Setting Up Camp. Auto-complete quest mentioned by Q3486 Empowered Tower.
    /// Quest 3671 -> grants 3668 on completion.
    /// </summary>
    [ScriptFilterOwnerId(3671u)]
    public class Q3671QuestScript : FollowUpQuestScript<Q3671QuestScript>
    {
        protected override ushort NextQuestId => 3668;

        public Q3671QuestScript(ILogger<Q3671QuestScript> log, IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
