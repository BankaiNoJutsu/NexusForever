using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Paths
{
    /// <summary>Exile Scientist: kill intro quest. Quest 10553.</summary>
    [ScriptFilterOwnerId(10553u)]
    public class Q10553ScientistKillQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10553ScientistKillQuestScript> log; private IQuest owner;
        public Q10553ScientistKillQuestScript(ILogger<Q10553ScientistKillQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
    /// <summary>Exile Scientist: spell success + activate quest. Quest 10554.</summary>
    [ScriptFilterOwnerId(10554u)]
    public class Q10554ScientistSpellQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10554ScientistSpellQuestScript> log; private IQuest owner;
        public Q10554ScientistSpellQuestScript(ILogger<Q10554ScientistSpellQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
    /// <summary>Exile Scientist: SucceedCSI quest. Quest 10555.</summary>
    [ScriptFilterOwnerId(10555u)]
    public class Q10555ScientistCsiQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10555ScientistCsiQuestScript> log; private IQuest owner;
        public Q10555ScientistCsiQuestScript(ILogger<Q10555ScientistCsiQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
}
