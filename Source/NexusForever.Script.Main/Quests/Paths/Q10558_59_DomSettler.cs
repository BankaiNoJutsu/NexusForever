using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Paths
{
    /// <summary>Dominion Settler: mass activate quest. Quest 10558.</summary>
    [ScriptFilterOwnerId(10558u)]
    public class Q10558DomSettlerActivateQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10558DomSettlerActivateQuestScript> log; private IQuest owner;
        public Q10558DomSettlerActivateQuestScript(ILogger<Q10558DomSettlerActivateQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
    /// <summary>Dominion Settler: SucceedCSI quest. Quest 10559.</summary>
    [ScriptFilterOwnerId(10559u)]
    public class Q10559DomSettlerCsiQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10559DomSettlerCsiQuestScript> log; private IQuest owner;
        public Q10559DomSettlerCsiQuestScript(ILogger<Q10559DomSettlerCsiQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
}
