using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Dominion holo-terminal quest.
    /// Quest 10541 → grants 10522 on completion.
    /// Objectives: ActivateEntity at terminal (WL 51709, 160m radius), EnterZone 4964.
    /// </summary>
    [ScriptFilterOwnerId(10541u)]
    public class Q10541HoloTerminalQuestScript : FollowUpQuestScript<Q10541HoloTerminalQuestScript>
    {
        protected override ushort NextQuestId => 10522;

        public Q10541HoloTerminalQuestScript(
            ILogger<Q10541HoloTerminalQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
