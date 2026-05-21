using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Script.Main.Quests.Paths
{
    /// <summary>Exile Soldier: kill intro quest. Quest 10544.</summary>
    [ScriptFilterOwnerId(10544u)]
    public class Q10544SoldierKillQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10544SoldierKillQuestScript> log; private IQuest owner;
        public Q10544SoldierKillQuestScript(ILogger<Q10544SoldierKillQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
    /// <summary>Exile Soldier: spell success + activate quest. Quest 10545.</summary>
    [ScriptFilterOwnerId(10545u)]
    public class Q10545SoldierSpellQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10545SoldierSpellQuestScript> log; private IQuest owner;
        public Q10545SoldierSpellQuestScript(ILogger<Q10545SoldierSpellQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
    /// <summary>Exile Soldier: mass activate quest. Quest 10546.</summary>
    [ScriptFilterOwnerId(10546u)]
    public class Q10546SoldierActivateQuestScript : IQuestScript, IOwnedScript<IQuest>
    {
        private readonly ILogger<Q10546SoldierActivateQuestScript> log; private IQuest owner;
        public Q10546SoldierActivateQuestScript(ILogger<Q10546SoldierActivateQuestScript> log) => this.log = log;
        public void OnLoad(IQuest o) { owner = o; log.LogDebug("Path {QuestId} loaded.", o.Id); }
        public void OnQuestStateChange(QuestState n, QuestState old) => log.LogDebug("Path {QuestId}: {Old} -> {New}.", owner.Id, old, n);
    }
}
