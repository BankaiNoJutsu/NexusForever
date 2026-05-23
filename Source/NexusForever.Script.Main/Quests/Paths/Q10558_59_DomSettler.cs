using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Paths
{
    /// <summary>Dominion Settler: mass activate quest. Quest 10558 -> 10559.</summary>
    [ScriptFilterOwnerId(10558u)]
    public class Q10558DomSettlerActivateQuestScript : FollowUpQuestScript<Q10558DomSettlerActivateQuestScript>
    {
        protected override ushort NextQuestId => 10559;
        public Q10558DomSettlerActivateQuestScript(ILogger<Q10558DomSettlerActivateQuestScript> log, IGlobalQuestManager m) : base(log, m) { }
    }
    /// <summary>Dominion Settler: SucceedCSI quest. Quest 10559 (terminal).</summary>
    [ScriptFilterOwnerId(10559u)]
    public class Q10559DomSettlerCSIQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10559DomSettlerCSIQuestScript> log; private IQuest owner;
        public Q10559DomSettlerCSIQuestScript(ILogger<Q10559DomSettlerCSIQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
}
