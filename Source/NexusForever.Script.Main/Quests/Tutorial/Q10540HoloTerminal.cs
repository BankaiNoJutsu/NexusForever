using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Tutorial
{
    /// <summary>
    /// Exile holo-terminal quest.
    /// Quest 10540 → grants 10519 on completion.
    /// Objectives: ActivateEntity at terminal (WL 51709, 160m radius), EnterZone 4964.
    /// </summary>
    [ScriptFilterOwnerId(10540u)]
    public class Q10540HoloTerminalQuestScript : FollowUpQuestScript<Q10540HoloTerminalQuestScript>
    {
        protected override ushort NextQuestId => 10519;

        public Q10540HoloTerminalQuestScript(
            ILogger<Q10540HoloTerminalQuestScript> log,
            IGlobalQuestManager globalQuestManager)
            : base(log, globalQuestManager)
        {
        }
    }
}
