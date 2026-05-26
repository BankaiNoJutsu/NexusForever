using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.NorthernWilds
{
    /// <summary>
    /// Northern Wilds/Galeras transition: Leaving the Temple of Osiric.
    /// </summary>
    [ScriptFilterOwnerId(4696u)]
    public class Q4696LeavingTheTempleOfOsiricQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        public void OnLoad(IQuest owner)
        {
        }
    }
}
