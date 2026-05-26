using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds: Off with Her Head! Item-started turn-in quest.
    /// </summary>
    [ScriptFilterOwnerId(3783u)]
    public class Q3783OffWithHerHeadQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        public void OnLoad(IQuest owner)
        {
        }
    }
}
