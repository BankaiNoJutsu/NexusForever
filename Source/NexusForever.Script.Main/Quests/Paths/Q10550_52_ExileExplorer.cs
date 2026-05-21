using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Paths
{
    /// <summary>Exile Explorer: kill intro quest. Quest 10550.</summary>
    [ScriptFilterOwnerId(10550u)]
    public class Q10550ExplorerKillQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10550ExplorerKillQuestScript> log; private IQuest owner;
        public Q10550ExplorerKillQuestScript(ILogger<Q10550ExplorerKillQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
    /// <summary>Exile Explorer: spell success + activate quest. Quest 10551.</summary>
    [ScriptFilterOwnerId(10551u)]
    public class Q10551ExplorerSpellQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10551ExplorerSpellQuestScript> log; private IQuest owner;
        public Q10551ExplorerSpellQuestScript(ILogger<Q10551ExplorerSpellQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
    /// <summary>Exile Explorer: SucceedCSI quest. Quest 10552.</summary>
    [ScriptFilterOwnerId(10552u)]
    public class Q10552ExplorerCsiQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10552ExplorerCsiQuestScript> log; private IQuest owner;
        public Q10552ExplorerCsiQuestScript(ILogger<Q10552ExplorerCsiQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
}
